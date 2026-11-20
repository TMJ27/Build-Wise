param(
    [string]$Profile = 'buildwise-admin',
    [string]$ImageTag = ('release-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
)
$ErrorActionPreference = 'Stop'
$env:AWS_PAGER = ''
$repo = Split-Path $PSScriptRoot -Parent
Set-Location $repo
function Invoke-AwsJson($service, $operation, $payload) {
    $path = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString() + '.json')
    try {
        [IO.File]::WriteAllText($path, ($payload | ConvertTo-Json -Depth 40), [Text.UTF8Encoding]::new($false))
        $result = & aws $service $operation --cli-input-json "file://$path" --profile $Profile --region eu-north-1 --output json
        if ($LASTEXITCODE -ne 0) { throw "AWS $service $operation failed" }
        if ($result) { $result | ConvertFrom-Json }
    } finally { Remove-Item -LiteralPath $path -ErrorAction SilentlyContinue }
}
function Register-Image($family) {
    $definition = (Invoke-AwsJson ecs describe-task-definition @{taskDefinition=$family}).taskDefinition
    foreach ($property in @('taskDefinitionArn','revision','status','requiresAttributes','compatibilities','registeredAt','registeredBy','deregisteredAt')) {
        $definition.PSObject.Properties.Remove($property)
    }
    $definition.containerDefinitions[0].image = "896932223680.dkr.ecr.eu-north-1.amazonaws.com/buildwise:$ImageTag"
    (Invoke-AwsJson ecs register-task-definition $definition).taskDefinition.taskDefinitionArn
}
& python deployment/package-source.py
if ($LASTEXITCODE -ne 0) { throw 'Source packaging failed' }
& aws s3 cp obj/buildwise-source.zip s3://buildwise-production-896932223680-eun1/source/buildwise-source.zip --sse AES256 --profile $Profile --region eu-north-1 --no-progress
if ($LASTEXITCODE -ne 0) { throw 'Source upload failed' }
$build = Invoke-AwsJson codebuild start-build @{projectName='buildwise-release';environmentVariablesOverride=@(@{name='IMAGE_TAG';value=$ImageTag;type='PLAINTEXT'})}
Write-Host "AWS build: $($build.build.id)"
do {
    Start-Sleep -Seconds 15
    $buildState = (Invoke-AwsJson codebuild batch-get-builds @{ids=@($build.build.id)}).builds[0]
    Write-Host "Build $($buildState.buildStatus): $($buildState.currentPhase)"
} while ($buildState.buildStatus -eq 'IN_PROGRESS')
if ($buildState.buildStatus -ne 'SUCCEEDED') { throw 'Cloud build failed; review /aws/codebuild/buildwise-release logs' }
$serviceArn = 'arn:aws:ecs:eu-north-1:896932223680:service/buildwise-production/buildwise'
$service = (Invoke-AwsJson ecs describe-express-gateway-service @{serviceArn=$serviceArn}).service
$network = $service.activeConfigurations[0].networkConfiguration
$migrationDefinition = Register-Image 'buildwise-migrations'
$migration = Invoke-AwsJson ecs run-task @{cluster='buildwise-production';taskDefinition=$migrationDefinition;launchType='FARGATE';count=1;networkConfiguration=@{awsvpcConfiguration=@{subnets=$network.subnets;securityGroups=$network.securityGroups;assignPublicIp='ENABLED'}}}
if ($migration.failures.Count -gt 0 -or $migration.tasks.Count -ne 1) { throw 'Migration task could not start' }
$taskArn = $migration.tasks[0].taskArn
& aws ecs wait tasks-stopped --cluster buildwise-production --tasks $taskArn --profile $Profile --region eu-north-1
if ($LASTEXITCODE -ne 0) { throw 'Migration task did not complete in time' }
$task = (Invoke-AwsJson ecs describe-tasks @{cluster='buildwise-production';tasks=@($taskArn)}).tasks[0]
if ($null -eq $task.containers[0].exitCode -or $task.containers[0].exitCode -ne 0) { throw 'Migration failed; application rollout was not started' }
$applicationDefinition = Register-Image 'buildwise-production'
Invoke-AwsJson ecs update-express-gateway-service @{serviceArn=$serviceArn;taskDefinitionArn=$applicationDefinition} | Out-Null
Write-Host "Rollout started: $applicationDefinition"
Write-Host 'Verify deployment stability, /health and sign-in before declaring the release complete.'
