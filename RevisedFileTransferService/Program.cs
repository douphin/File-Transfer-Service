using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using RevisedFileTransferService;

// More information about services at
// https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service?pivots=dotnet-8-0

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "File Transfer Service";
    })
    .ConfigureServices((context, services) =>
    {
        LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);

        services.AddHostedService<WindowsBackgroundService>();

        // See: https://github.com/dotnet/runtime/issues/47303
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(
                context.Configuration.GetSection("Logging"));
        });
    });

IHost host = builder.Build();
host.Run();