using Regnskap.Application.Features.Kundereskontro;
using Regnskap.Domain.Features.Kundereskontro;
using Regnskap.Infrastructure.Features.Kundereskontro;

namespace Regnskap.Api.Features.Kundereskontro;

public static class KundeServiceExtensions
{
    public static IServiceCollection AddKundereskontro(this IServiceCollection services)
    {
        services.AddScoped<IKundeReskontroRepository, KundeReskontroRepository>();
        services.AddScoped<IKundeService, KundeService>();
        services.AddScoped<IKundeFakturaService, KundeFakturaService>();
        services.AddScoped<IKidService, KidService>();
        services.AddScoped<IPurringService, PurringService>();
        return services;
    }
}
