using Regnskap.Application.Features.Kontoplan;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Infrastructure.Features.Kontoplan;

namespace Regnskap.Api.Features.Kontoplan;

public static class KontoplanServiceExtensions
{
    public static IServiceCollection AddKontoplan(this IServiceCollection services)
    {
        services.AddScoped<IKontoplanRepository, KontoplanRepository>();
        services.AddScoped<IKontoService, KontoService>();
        services.AddScoped<IMvaKodeService, MvaKodeService>();
        services.AddScoped<IKontoplanImportExportService, KontoplanImportExportService>();
        return services;
    }
}
