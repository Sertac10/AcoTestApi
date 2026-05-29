using Microsoft.Extensions.DependencyInjection;
using AcoTestApi.Application.Common.Interfaces;
using AcoTestApi.Infrastructure.Services;
using AcoTestApi.Infrastructure.Services.Channels;

namespace AcoTestApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPredictionService, PredictionService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        
        // Register concrete connection channels (Strategy Pattern)
        services.AddTransient<UsbPrinterChannel>();
        services.AddTransient<LanPrinterChannel>();
        services.AddSingleton<PrinterChannelFactory>();
        
        services.AddSingleton<IThermalPrinter, ThermalPrinterSimulator>();

        // Register PrintQueueService as both a Singleton and a HostedService to avoid captive dependency and multiple workers
        services.AddSingleton<PrintQueueService>();
        services.AddSingleton<IPrintQueueService>(sp => sp.GetRequiredService<PrintQueueService>());
        services.AddHostedService(sp => sp.GetRequiredService<PrintQueueService>());

        return services;
    }
}
