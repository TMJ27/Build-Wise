using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Rights;
using BuildWise.Web.Services;
using BuildWise.Web.Services.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static ClaimsPrincipal Account(string email, bool verified = true) => new(new ClaimsIdentity(new[]
{
    new Claim("email", email),
    new Claim("email_verified", verified.ToString().ToLowerInvariant())
}, "test"));

await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
var settings = new CognitoSettings();
var service = new CurrentUserService(new HttpContextAccessor(), db, settings);
Check(await service.EnsureUserExistsAsync(Account("owner@company.test", false)) is null,
    "Unverified account cannot claim setup.");
Check(!await db.Users.AnyAsync(), "Unverified account must not be inserted.");
var firstId = await service.EnsureUserExistsAsync(Account("owner@company.test"));
Check(firstId.HasValue, "First verified company user must be activated without a predefined email.");
var owner = await db.Users.Include(user => user.Group).SingleAsync();
Check(owner.IsActive && owner.Group?.RightsId == (int)AccessRights.All
    && owner.Group.CreatorId == owner.Id, "Setup owner receives all rights with a valid creator.");
Check(await service.EnsureUserExistsAsync(Account("employee@company.test")) is null,
    "Later users must await approval.");
Check(await db.UserGroups.CountAsync() == 1, "Do not create another administrators group.");
Check(await service.EnsureUserExistsAsync(Account("OWNER@company.test")) == firstId,
    "Repeated login must reuse owner.");
owner.IsActive = false;
await db.SaveChangesAsync();
Check(await service.EnsureUserExistsAsync(Account("owner@company.test")) is null,
    "Disabled setup administrator must not reactivate.");
Check(await service.EnsureUserExistsAsync(Account("stranger@company.test")) is null,
    "No new setup owner when all users have been disabled.");
await using var pendingDb = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
var pending = new User
{
    Id = Guid.NewGuid(), Email = "pending@company.test", IsActive = false,
    PasswordHash = "unused", CreationDate = DateTime.UtcNow
};
pendingDb.Users.Add(pending);
await pendingDb.SaveChangesAsync();
var pendingService = new CurrentUserService(new HttpContextAccessor(), pendingDb, new CognitoSettings());
Check(await pendingService.EnsureUserExistsAsync(Account(pending.Email)) == pending.Id,
    "Existing pending first account must be able to claim setup.");
Check(pending.IsActive && pending.Group?.RightsId == (int)AccessRights.All,
    "Pending setup account must receive all rights.");
Check(await pendingDb.Users.CountAsync() == 1, "Do not duplicate the pending account.");
Console.WriteLine("Passed: first verified owner, full rights, existing pending owner, later-user approval, duplicate prevention and no bootstrap after deactivation. Isolated in-memory databases only.");
