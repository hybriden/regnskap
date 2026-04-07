using Regnskap.Application.Features.Leverandorreskontro;
using Regnskap.Domain.Features.Leverandorreskontro;
using Regnskap.Infrastructure.Features.Leverandorreskontro;

namespace Regnskap.Api.Features.Leverandor;

public static class LeverandorServiceExtensions
{
    public static IServiceCollection AddLeverandor(this IServiceCollection services)
    {
        services.AddScoped<ILeverandorReskontroRepository, LeverandorReskontroRepository>();
        services.AddScoped<ILeverandorService, LeverandorService>();
        services.AddScoped<ILeverandorFakturaService, LeverandorFakturaService>();
        services.AddScoped<IBetalingsforslagService, BetalingsforslagService>();
        return services;
    }
}
