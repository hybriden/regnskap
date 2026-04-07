using Regnskap.Application.Features.Mva;
using Regnskap.Domain.Features.Mva;
using Regnskap.Infrastructure.Features.Mva;

namespace Regnskap.Api.Features.Mva;

public static class MvaServiceExtensions
{
    public static IServiceCollection AddMva(this IServiceCollection services)
    {
        services.AddScoped<IMvaRepository, MvaRepository>();
        services.AddScoped<IMvaTerminService, MvaTerminService>();
        services.AddScoped<IMvaOppgjorService, MvaOppgjorService>();
        services.AddScoped<IMvaAvstemmingService, MvaAvstemmingService>();
        services.AddScoped<IMvaMeldingService, MvaMeldingService>();
        services.AddScoped<IMvaSammenstillingService, MvaSammenstillingService>();
        services.AddScoped<ISaftTaxTableService, SaftTaxTableService>();
        return services;
    }
}
