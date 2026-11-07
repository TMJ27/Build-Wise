using BuildWise.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

if (args.Contains("--script"))
{
    // Generate a reviewable SQL artifact only. Never connect to or update a database here.
    await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
        .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=BuildWise_ScriptOnly;Trusted_Connection=True;TrustServerCertificate=True").Options);
    var migrations = db.Database.GetMigrations().ToArray();
    var sql = db.GetService<IMigrator>().GenerateScript(migrations[^2], migrations[^1], MigrationsSqlGenerationOptions.Idempotent);
    var directory = Path.GetFullPath("database/customer-portal");
    Directory.CreateDirectory(directory);
    var path = Path.Combine(directory, "CustomerPortalAccounts.sql");
    await File.WriteAllTextAsync(path, sql);
    Console.WriteLine(path);
    return;
}

await CustomerPortalChecks.RunAsync();
