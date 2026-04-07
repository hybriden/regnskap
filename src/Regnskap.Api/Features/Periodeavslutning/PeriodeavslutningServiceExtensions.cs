using Regnskap.Application.Features.Periodeavslutning;
using Regnskap.Infrastructure.Features.Periodeavslutning;

namespace Regnskap.Api.Features.Periodeavslutning;

public static class PeriodeavslutningServiceExtensions
{
    public static IServiceCollection AddPeriodeavslutning(this IServiceCollection services)
    {
        services.AddScoped<IPeriodeavslutningRepository, PeriodeavslutningRepository>();
        services.AddScoped<IPeriodeavslutningService, PeriodeavslutningService>();
        services.AddScoped<IAvskrivningService, AvskrivningService>();
        services.AddScoped<IPeriodiseringsService, PeriodiseringsService>();
        return services;
    }
}
