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
        _logg.LogInformation("Р¤РѕРЅРѕРІР°СЏ СЃР»СѓР¶Р±Р° Р»РѕРіРёСЂРѕРІР°РЅРёСЏ СЂР°Р±РѕС‚Р°РµС‚.");
await Task.Delay(100000,stoppingToken);
    }

    }
}

