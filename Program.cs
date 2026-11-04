using BuildWise.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<BuildWiseDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("BuildWise")
        ?? throw new InvalidOperationException(
            "The BuildWise database connection string is not configured.");

    options.UseSqlServer(connectionString);
});

builder.Services.AddSingleton<BuildWise.Web.Services.AccessWorkspace>();
builder.Services.AddScoped<BuildWise.Web.Services.CompanyService>();
builder.Services.AddSingleton<BuildWise.Web.Services.CatalogueWorkspace>();
builder.Services.AddSingleton<BuildWise.Web.Services.CustomerWorkspace>();
builder.Services.AddSingleton<BuildWise.Web.Services.RequestWorkspace>();
builder.Services.AddSingleton<BuildWise.Web.Services.QuotationWorkspace>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseStaticFiles();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
