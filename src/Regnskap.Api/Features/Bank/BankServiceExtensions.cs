using Regnskap.Application.Features.Bank;
using Regnskap.Domain.Features.Bankavstemming;
using Regnskap.Infrastructure.Features.Bank;

namespace Regnskap.Api.Features.Bank;

public static class BankServiceExtensions
{
    public static IServiceCollection AddBank(this IServiceCollection services)
    {
        services.AddScoped<IBankRepository, BankRepository>();
        services.AddScoped<ICamt053ImportService, Camt053ImportService>();
        services.AddScoped<IBankMatchingService, BankMatchingService>();
        services.AddScoped<IBankavstemmingService, BankavstemmingService>();
        return services;
    }
}
