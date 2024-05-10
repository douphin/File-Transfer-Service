using System.Text.Json;

namespace RevisedFileTransferService;

public sealed class WindowsBackgroundService : BackgroundService
{
    // This is dependency Injection stuff that I don't really understand, but allows messages to be submitted to the event viewer
    private readonly ILogger<WindowsBackgroundService> _logger;

    public WindowsBackgroundService(ILogger<WindowsBackgroundService> logger) =>
        (_logger) = (logger);

    // This is the main loop, that will execute when the service starts
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            const string transferlist_filename = @"C:\USR\SRC\CS\RevisedFileTransferService\FileTransferList.json";
            const string status_filename = @"C:\USR\SRC\CS\RevisedFileTransferService\FileTransferStatus.json";
            
            // The JSON is set up to mimic TransferSerializer.cs so it's able to drop everything into place
            string JSONstring = File.ReadAllText(transferlist_filename);
            TransferSerializer? ServiceData = JsonSerializer.Deserialize<TransferSerializer>(JSONstring);

            ServiceData.StartTimers();

            // UnhandledErrors.txt is a static text file used to catch errors
            using (StreamWriter file = new StreamWriter(@"C:\USR\Logs\File Transfer Logs\UnhandledErrors.txt", true))
            {
                file.WriteLine();
                file.WriteLine(DateTime.Now.ToString("t") + "_Service Start");
                file.WriteLine();
            }

            _logger.LogWarning("Service Started without errors");

            await Task.Delay(TimeSpan.FromMinutes(6), stoppingToken);

            // This loop will run continuously until it hits an unhandled error, or until the stop button is pressed in the services menu
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    JSONstring = File.ReadAllText(transferlist_filename);
                    TransferSerializer? JSONdata = JsonSerializer.Deserialize<TransferSerializer>(JSONstring);

                    // JSONdata.UpdateService is a boolean held in the JSON that is used to indicate when the service should reread the JSON to account for any changes
                    if (JSONdata.UpdateService)
                    {
                        ServiceData.StopTimers();

                        // Update the data for the service with the new data from the JSON
                        ServiceData = JSONdata;

                        // Change the JSON to indicate that the new data has been accepted, update the lastUpdated time
                        ServiceData.UpdateService = false;
                        ServiceData.LastUpdate = DateTime.Now.ToString("f");

                        JSONstring = JsonSerializer.Serialize(ServiceData, new JsonSerializerOptions { WriteIndented = true } );
                        File.WriteAllText(transferlist_filename, JSONstring);

                        ServiceData.StartTimers();
                    }
                }
                catch(Exception ex)
                {
                    using (StreamWriter file = new StreamWriter(@"C:\USR\Logs\File Transfer Logs\UnhandledErrors.txt", true))
                    {
                        file.WriteLine();
                        file.WriteLine(DateTime.Now.ToString("t") + "-" + ex.Message);
                        file.WriteLine();
                    }
                }

                // Wait 6 minutes because most copy methods are set to copy every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(2.5), stoppingToken);

                // Basically whats going to happen is a JSON that holds the status of different parts of the service will get updated with the last known status of each method
                try
                {
                    JSONstring = File.ReadAllText(status_filename);

                    // This wil serialize according to StatusSerializer.cs
                    StatusSerializer? ServiceStatus = JsonSerializer.Deserialize<StatusSerializer>(JSONstring);

                    ServiceStatus.statusObjects = ServiceData.ReturnStatusList();
                    ServiceStatus.LastUpdated = DateTime.Now.ToString("f");

                    JSONstring = JsonSerializer.Serialize(ServiceStatus, new JsonSerializerOptions { WriteIndented = true });

                    File.WriteAllText(status_filename, JSONstring);
                }
                catch(Exception ex)
                {
                    using (StreamWriter file = new StreamWriter(@"C:\USR\Logs\File Transfer Logs\UnhandledErrors.txt", true))
                    {
                        file.WriteLine();
                        file.WriteLine(DateTime.Now.ToString("t") + "_" + ex.ToString());
                        file.WriteLine();
                    }
                }
            }

        }
        catch (TaskCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }
}