using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Caching.Memory;
using MyApiBlya.Services;
public class Adding : IAddAction
{
    private readonly AppDbContext _context; 
    private readonly IMemoryCache _cache;
    private readonly ILogger<Adding> _logger;

    public Adding(AppDbContext context,IMemoryCache cache, ILogger<Adding> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }
 public  async Task AddActions(User user   , string act)
    {
if (!user.actions.Contains(act))
{
    user.actions.Add(act);
}

var action  = await _context.histories.FirstOrDefaultAsync(x=>x.Action==act);
        if (action == null)
        {
             action = new History()
            {
                Action = act
            };
            await _context.histories.AddAsync(action);
                    await _context.SaveChangesAsync(); 
            
        }
var history =new UsersHistory(){
            users_id  =user.Id,
            actions_id = action!.Id
        };

await _context.UsersHistory.AddAsync(history);
await _context.SaveChangesAsync(); 
_cache.Remove(CacheKeys.UserHistory(user.Id));
_logger.LogInformation("Добавлена история действия пользователя. Идентификатор пользователя: {UserId}, действие: {Action}", user.Id, act);

        

    }
}
