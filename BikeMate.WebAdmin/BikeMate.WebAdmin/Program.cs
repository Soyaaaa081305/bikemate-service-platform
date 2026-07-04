using BikeMate.WebAdmin.Components;
using BikeMate.WebAdmin.Services;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Configuration
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

// Add services
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("BikeMateDb")
    ?? "Server=localhost\\SQLEXPRESS;Database=BikeMatesDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;Command Timeout=300;";

builder.Services.AddDbContext<BikeMateDbContext>(options =>
    options.UseSqlServer(connectionString));

// UI data service
builder.Services.AddScoped<AdminApiClient>();
builder.Services.AddScoped<IWebAdminAgoraCallService, WebAdminAgoraCallService>();

// AUTHENTICATION & AUTHORIZATION SETUP
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    });

// Use AddAuthorization (Do NOT use AddAuthorizationCore with AddAuthentication)
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// CRITICAL: These must be before MapRazorComponents
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
