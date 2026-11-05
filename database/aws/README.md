# Deploy BuildWise with EF Core migrations

The AWS RDS instance hosts SQL Server. The deployment process runs BuildWise's
migration command before releasing the web application. SSMS is optional.

## Required deployment secrets

- Migration job: `ConnectionStrings__BuildWiseMigrations` targets the RDS
  `BuildWise` database and uses a deployment login with schema permissions.
  On the first deployment, creating the database also requires database-creation
  permission. The RDS administrative login can perform that initial setup.
- Web application: `ConnectionStrings__BuildWise` targets the same database,
  using a separate application login with the permissions required at runtime.

Example format; replace placeholders in the AWS secret configuration, not here:

```text
Server=tcp:<RDS-endpoint>,1433;Database=BuildWise;User ID=<login>;Password=<secret>;Encrypt=True;TrustServerCertificate=False;
```

The host needs to trust the current Amazon RDS certificate chain.

## Release sequence

1. Build and publish the release artifact or Docker image.
2. Run one migration job using that exact release artifact inside a network
   that can reach the database.
3. Supply its migration connection string through the deployment secret store.
4. Run:

   ```sh
   dotnet BuildWise.Web.dll --migrate
   ```

   For the existing Docker image, keep the normal entrypoint and pass
   `--migrate` as the container command. The command applies pending EF migrations
   and exits without starting the web server. EF creates the database if it
   does not exist and the deployment login has permission.
5. Continue the release only after the migration process exits with code 0.
   If it fails, stop the release and inspect the migration logs.
6. Start/update the normal web application using:

   ```sh
   dotnet BuildWise.Web.dll
   ```

The migration job and application must point at the same RDS database.
Only the migration job needs the migration secret. Normal application startup
does not apply schema changes. Re-running a successful job skips migrations
recorded in `__EFMigrationsHistory`.

The first deployment creates tables without copying local company or user data.
Later deployments apply newly committed migration files. Add and review a
migration whenever entity mappings change; publishing code alone does not
generate new migrations.

## AWS status

The migration entrypoint is ready. It has not been run against RDS. No AWS
pipeline or hosting service has been configured in this repository yet.
Wire the migration job into the chosen hosting service's release process
before the first web release. Keep the database private when both jobs and
the web app can reach it within the VPC.

## Schema review

`BuildWise.Schema.sql` is an optional SQL review artifact generated from the same
migrations; it is not a second required manual deployment step. The added
`RestoreCurrencyCodeUniqueness` migration restores the unique code index
removed by the previous migration. On an existing database, duplicate codes
must be resolved before that index can be created.

Review and test migrations on a staging database before a live release.
An EF migration history mismatch in a pre-existing manually created database
must be resolved before using the migration job.

Reference:
https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying
