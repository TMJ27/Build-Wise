using BuildWise.Data;
using Microsoft.EntityFrameworkCore;
using BuildWise.Web.Services.Authentication;
using BuildWise.Web.Services.CustomerPortal;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;

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
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    if (builder.Configuration["Hosting:TrustedProxyNetwork"] is { Length: > 0 } network)
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
});
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    Directory.CreateDirectory(keysPath);
    builder.Services.AddDataProtection().SetApplicationName("BuildWise")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BuildWise.Lib.Users.ICurrentUserService, BuildWise.Web.Services.CurrentUserService>();
builder.Services.AddCognitoAuthentication(builder.Configuration);
builder.Services.AddCustomerPortal();
builder.Services.AddScoped<BuildWise.Web.Services.Email.IEmailSender, BuildWise.Web.Services.Email.SesEmailSender>();
builder.Services.AddHttpClient<BuildWise.Web.Services.Scopes.GeminiScopeDraftService>(client =>
{
    // The service applies one deadline to both the headers and response body.
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddHttpClient<BuildWise.Web.Services.Quotations.GeminiQuotationWordingService>(client =>
{
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddHttpClient<BuildWise.Web.Services.Quotations.GeminiQuotationReviewService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddDbContext<BuildWiseDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("BuildWise")
        ?? throw new InvalidOperationException(
            "The BuildWise database connection string is not configured.");

    options.UseSqlServer(connectionString);
});


var app = builder.Build();

// Deployment runs migrations explicitly; development keeps its existing startup behaviour.
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<BuildWiseDbContext>();
    await database.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
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

app.MapGet("/health", async (BuildWiseDbContext database) =>
    await database.Database.CanConnectAsync() ? Results.Ok("Healthy") : Results.StatusCode(503))
    .AllowAnonymous();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
