using BuildWise.Data;
using Microsoft.EntityFrameworkCore;
using BuildWise.Web.Services.Authentication;
using BuildWise.Web.Services.CustomerPortal;

var migrateOnly = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);
var builder = WebApplication.CreateBuilder(args
    .Where(argument => !argument.Equals("--migrate", StringComparison.OrdinalIgnoreCase))
    .ToArray());

if (migrateOnly)
{
    var connectionString = builder.Configuration.GetConnectionString("BuildWiseMigrations");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("Set ConnectionStrings__BuildWiseMigrations in the deployment secrets before running migrations.");
        Environment.ExitCode = 1;
        return;
    }

    var options = new DbContextOptionsBuilder<BuildWiseDbContext>()
        .UseSqlServer(connectionString)
        .Options;

    await using var database = new BuildWiseDbContext(options);
    await database.Database.MigrateAsync();
    Console.WriteLine("BuildWise database migrations completed successfully.");
    return;
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BuildWise.Lib.Users.ICurrentUserService, BuildWise.Web.Services.CurrentUserService>();
builder.Services.AddCognitoAuthentication(builder.Configuration);
builder.Services.AddCustomerPortal();
builder.Services.AddDbContext<BuildWiseDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("BuildWise")
        ?? throw new InvalidOperationException(
            "The BuildWise database connection string is not configured.");

    options.UseSqlServer(connectionString);
});

builder.Services.AddSingleton<BuildWise.Web.Services.AccessWorkspace>();
builder.Services.AddSingleton<BuildWise.Web.Services.CatalogueWorkspace>();
builder.Services.AddSingleton<BuildWise.Web.Services.CustomerWorkspace>();
builder.Services.AddSingleton<BuildWise.Web.Services.RequestWorkspace>();
builder.Services.AddSingleton<BuildWise.Web.Services.QuotationWorkspace>();

var app = builder.Build();

// Apply existing EF migrations before serving requests.
await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<BuildWiseDbContext>();
    await database.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseRateLimiter();

app.UseStaticFiles();

app.UseAuthentication();
app.UseMiddleware<BuildWise.Web.Middleware.UserProvisioningMiddleware>();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
