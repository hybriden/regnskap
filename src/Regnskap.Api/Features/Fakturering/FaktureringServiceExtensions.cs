using Regnskap.Application.Features.Fakturering;
using Regnskap.Domain.Features.Fakturering;
using Regnskap.Infrastructure.Features.Fakturering;

namespace Regnskap.Api.Features.Fakturering;

public static class FaktureringServiceExtensions
{
    public static IServiceCollection AddFakturering(this IServiceCollection services)
    {
        services.AddScoped<IFakturaRepository, FakturaRepository>();
        services.AddScoped<IFaktureringService, FaktureringService>();
        services.AddScoped<IEhfService, EhfService>();
        services.AddScoped<IFakturaPdfService, FakturaPdfService>();
        return services;
    }
}
