using MyApiBlya.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;
public class BackgroundLoggingService : BackgroundService
{
     private readonly   ILogger<BackgroundLoggingService>_logg; 
      private readonly IServiceScopeFactory _scope;
     public BackgroundLoggingService(ILogger<BackgroundLoggingService> logg,IServiceScopeFactory scope)
    {_scope = scope;
        _logg =  logg; 
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {while(!stoppingToken.IsCancellationRequested){
        _logg.LogInformation("Фоновая служба логирования работает.");
await Task.Delay(100000,stoppingToken);
await RemoveOldActions(stoppingToken);
    }

    }
    private  async Task RemoveOldActions(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {await using var  scope = _scope.CreateAsyncScope();
        var border = DateTime.UtcNow.AddDays(-1);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var UserHistories = await db.UserActionHistories.Where(x=>x.CreatedAt< border).ToListAsync();
            foreach(var item in UserHistories)
            {
                if (item.CreatedAt < border)
                {
                    db.UserActionHistories.RemoveRange(item);
                }
            }
            await db.SaveChangesAsync();
            await Task.Delay(100,cancellationToken:stoppingToken);
        }
    }
}

