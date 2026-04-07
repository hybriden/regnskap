using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Regnskap.Api.Features.Kontoplan;
using Regnskap.Api.Features.Hovedbok;
using Regnskap.Api.Features.Bilagsregistrering;
using Regnskap.Api.Features.Mva;
using Regnskap.Api.Features.Kundereskontro;
using Regnskap.Api.Features.Leverandor;
using Regnskap.Api.Features.Bank;
using Regnskap.Api.Features.Fakturering;
using Regnskap.Api.Features.Periodeavslutning;
using Regnskap.Api.Features.Rapportering;
using Regnskap.Infrastructure.Persistence;
using Regnskap.Infrastructure.Features.Kontoplan;

var builder = WebApplication.CreateBuilder(args);

// Database — use InMemory for dev, PostgreSQL for production
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString) || builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<RegnskapDbContext>(options =>
        options.UseInMemoryDatabase("RegnskapDev"));
}
else
{
    builder.Services.AddDbContext<RegnskapDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Authentication — disabled in dev for easy testing
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "regnskap-api",
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "regnskap-client",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DefaultDevKey_MustBeAtLeast32BytesLong!!"))
            };
        });
}
else
{
    // Dev: no-op auth that allows all requests
    builder.Services.AddAuthentication("Dev")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevAuthHandler>("Dev", null);
}
builder.Services.AddAuthorization();

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Modules
builder.Services.AddKontoplan();
builder.Services.AddHovedbok();
builder.Services.AddBilag();
builder.Services.AddMva();
builder.Services.AddKundereskontro();
builder.Services.AddLeverandor();
builder.Services.AddBank();
builder.Services.AddFakturering();
builder.Services.AddPeriodeavslutning();
builder.Services.AddRapportering();

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Seed data in dev
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RegnskapDbContext>();
    db.Database.EnsureCreated();
    KontoplanSeedData.Seed(db);
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

app.Run();

// Dev auth handler — allows all requests without token
public class DevAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
{
    public DevAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new System.Security.Claims.ClaimsIdentity("Dev");
        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "dev-user"));
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Dev");
        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}

// Make Program accessible for integration tests
public partial class Program { }
