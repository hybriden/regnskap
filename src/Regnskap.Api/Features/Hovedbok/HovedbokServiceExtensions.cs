using Regnskap.Application.Features.Hovedbok;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Infrastructure.Features.Hovedbok;

namespace Regnskap.Api.Features.Hovedbok;

public static class HovedbokServiceExtensions
{
    public static IServiceCollection AddHovedbok(this IServiceCollection services)
    {
        services.AddScoped<IHovedbokRepository, HovedbokRepository>();
        services.AddScoped<IHovedbokService, HovedbokService>();
        services.AddScoped<IPeriodeService, PeriodeService>();
        return services;
    }
}
