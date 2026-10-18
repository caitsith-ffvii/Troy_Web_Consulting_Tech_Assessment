using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Identity;
using PropertyManagement.Web.Data;
using PropertyManagement.Web.Services;
using PropertyManagement.Web.ViewModels.RentalApplications;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    // Statuses and sort fields serialize as names in the JSON API.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<RentalApplicationService>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<PropertyService>();
builder.Services.AddScoped<UnitService>();
builder.Services.AddScoped<ApplicationListService>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddTransient<RentalApplicationViewModelFactory>();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// API callers get status codes, not redirects to the login or
// access-denied pages.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
        RespondOrRedirect(context, StatusCodes.Status401Unauthorized);

    options.Events.OnRedirectToAccessDenied = context =>
        RespondOrRedirect(context, StatusCodes.Status403Forbidden);

    static Task RespondOrRedirect(
        Microsoft.AspNetCore.Authentication.RedirectContext<
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions> context,
        int apiStatusCode)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = apiStatusCode;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    }
});

var app = builder.Build();

// The test host turns this off; the test database is migrated by the tests.
if (app.Configuration.GetValue("Database:InitializeOnStartup", defaultValue: true))
{
    using var scope = app.Services.CreateScope();

    await scope.ServiceProvider
        .GetRequiredService<DatabaseInitializer>()
        .InitializeAsync();
}

if (app.Environment.IsDevelopment())
{
    // OpenAPI document at /openapi/v1.json, browsable at /swagger.
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "Property Management API"));
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Exposes the entry point to WebApplicationFactory in the tests.
public partial class Program;
