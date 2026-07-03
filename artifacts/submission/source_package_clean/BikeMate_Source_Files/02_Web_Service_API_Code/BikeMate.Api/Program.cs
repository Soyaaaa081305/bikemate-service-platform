using System.Text;
using BikeMate.Api.Hubs;
using BikeMate.Api.Middleware;
using BikeMate.Api.Services;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedHost |
        ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["https://localhost:5001", "https://10.0.2.2:5001"];
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithOrigins(allowedOrigins);
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=BikeMatesDB;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<BikeMateDbContext>(options =>
    options.UseSqlServer(connectionString));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("FATAL: Jwt:Key is not configured. Set it in appsettings or environment variables.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BikeMate";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BikeMateMobile";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IBookingConversationService, BookingConversationService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IAgoraTokenService, AgoraTokenService>();
builder.Services.AddScoped<IPayMongoService, PayMongoService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAdminReportService, AdminReportService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (args.Any(arg => string.Equals(arg, "--migrate", StringComparison.OrdinalIgnoreCase)))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<BikeMateDbContext>();
    await database.Database.MigrateAsync();
    Console.WriteLine("BikeMate database migrations applied.");
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseStaticFiles();
app.UseCors("MobileApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BookingHub>("/hubs/booking").RequireAuthorization();
app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();
app.MapHub<EmergencyHub>("/hubs/emergency").RequireAuthorization();
app.MapHub<LocationHub>("/hubs/location").RequireAuthorization();
app.MapHub<NotificationHub>("/hubs/notification").RequireAuthorization();

app.Run();
