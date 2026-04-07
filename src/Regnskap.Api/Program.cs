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
using Regnskap.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<RegnskapDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpContextAccessor (M-1: brukeridentitet i audit trail)
builder.Services.AddHttpContextAccessor();

// Authentication & Authorization (M-5: JWT Bearer)
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
builder.Services.AddAuthorization();

// Modules
builder.Services.AddKontoplan();
builder.Services.AddHovedbok();
builder.Services.AddBilag();
builder.Services.AddMva();
builder.Services.AddKundereskontro();
builder.Services.AddLeverandor();
builder.Services.AddBank();
builder.Services.AddFakturering();

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
