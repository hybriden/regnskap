using Regnskap.Application.Features.Rapportering;
using Regnskap.Infrastructure.Features.Rapportering;

namespace Regnskap.Api.Features.Rapportering;

public static class RapporteringServiceExtensions
{
    public static IServiceCollection AddRapportering(this IServiceCollection services)
    {
        services.AddScoped<IRapporteringRepository, RapporteringRepository>();
        services.AddScoped<IRapporteringService, RapporteringService>();
        services.AddScoped<ISaftEksportService, SaftEksportService>();
        services.AddScoped<ISaftDataProvider, SaftDataProvider>();
        services.AddScoped<IBudsjettService, BudsjettService>();
        return services;
    }
}
