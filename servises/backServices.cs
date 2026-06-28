using MyApiBlya.Services;
using Microsoft.Extensions.Hosting;
public class BackgroundLoggingService : BackgroundService
{
     private readonly   ILogger<BackgroundLoggingService>_logg; 
     public BackgroundLoggingService(ILogger<BackgroundLoggingService> logg)
    {
        _logg =  logg; 
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {while(!stoppingToken.IsCancellationRequested){
        _logg.LogInformation("Фоновая служба логирования работает.");
await Task.Delay(100000,stoppingToken);
    }

    }
}

