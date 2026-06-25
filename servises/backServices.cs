using MyApiBlya.Services;
using Microsoft.Extensions.Hosting;
public class LoggService : BackgroundService
{
     private readonly   ILogger<LoggService>_logg; 
     public LoggService(ILogger<LoggService> logg)
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
