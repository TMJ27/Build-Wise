# BuildWise production

Production runs separately from local development in AWS account `896932223680`, region `eu-north-1`.

- ECS cluster: `buildwise-production`, Express service: `buildwise`.
- URL: https://bu-ed357d6290684197aa19d3e4419a88d1.ecs.eu-north-1.on.aws
- ECR repository: `896932223680.dkr.ecr.eu-north-1.amazonaws.com/buildwise`.
- RDS SQL Server: `database-1`, database `BuildWise`.
- Database connection: Secrets Manager `buildwise/production/database`.
- Migration administrator: Secrets Manager `buildwise/production/database-admin`, accessible through the separate migration execution role. The application uses SQL login `buildwise_app` with read/write roles, without database-owner or server-administrator privileges.
- Cognito production app client: `6limjar2j78ofbmadglnsffe3h`; client secret: `buildwise/production/cognito`.
- AI key: Secrets Manager `buildwise/production/gemini`.
- First administrator is restricted to verified Cognito email `tavomada27@gmail.com`.
- Logs: `/ecs/buildwise`, retained for 14 days.
- EFS `fs-005a585fa0f544330`: separate access points for `/app/keys` and `/app/wwwroot/logos`, encrypted in transit and at rest, automatic backups enabled.
- Local timestamps use `Africa/Harare` through the container's `TZ` setting.

No production connection strings or passwords belong in the repository or local User Secrets. ECS retrieves secrets through its execution role. The application task role handles SES sending and scoped EFS access.

## Release procedure

Run `powershell -ExecutionPolicy Bypass -File deployment/Deploy-Release.ps1` with an authenticated `buildwise-admin` profile. It packages source, builds inside AWS, runs the migration task, checks its exit code and starts the application rollout with a unique image tag. The steps below describe the same process.

1. Build the production Dockerfile, then push a uniquely tagged image to ECR. The `buildwise-release` CodeBuild project can build inside AWS from the private S3 source archive at `buildwise-production-896932223680-eun1/source/buildwise-source.zip`; its buildspec is [buildspec.yml](buildspec.yml). Source objects expire after seven days. Exclude local secrets, development configuration, build outputs, and tests from that archive. The checked-in RDS certificates come from AWS's official `eu-north-1` trust bundle; refresh them when AWS announces CA rotation.
2. Register a new migration task definition using the image and AWS-injected `ConnectionStrings__BuildWiseMigrations`. Run `dotnet BuildWise.Web.dll --migrate` on the deployment network; require exit code 0 before promoting the application.
3. Register a new application task definition preserving EFS mounts, secret references, production URL and trusted proxy CIDR. Keep the main container named `Main` with its single named TCP port on 8080.
4. Update the Express service using the new task-definition ARN. Check the service deployment, `/health`, customer sign-in/recovery forms and staff Cognito redirect before declaring success.

Application migrations are disabled on production startup. Development retains its existing configuration and startup migrations. Production does not automatically copy local business data.

SES remains in sandbox; recipient identities must be verified until AWS grants production access. New production company and email settings must be saved through the configuration pages after administrator setup.
