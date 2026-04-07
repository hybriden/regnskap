using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Application.Features.Hovedbok;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Infrastructure.Features.Bilagsregistrering;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Api.Features.Bilagsregistrering;

public static class BilagServiceExtensions
{
    public static IServiceCollection AddBilag(this IServiceCollection services)
    {
        services.AddScoped<IBilagRepository, BilagRepository>();
        services.AddScoped<ITransactionManager, EfTransactionManager>();
        services.AddScoped<BilagRegistreringService>();
        services.AddScoped<IBilagRegistreringService>(sp => sp.GetRequiredService<BilagRegistreringService>());
        services.AddScoped<IBilagService>(sp => sp.GetRequiredService<BilagRegistreringService>());
        services.AddScoped<BilagSokService>();
        return services;
    }
}
