using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Caching.Memory;
using MyApiBlya.Services;
public class UserActionService : IUserActionService
{
    private readonly AppDbContext _context; 
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserActionService> _logger;

    public UserActionService(AppDbContext context,IMemoryCache cache, ILogger<UserActionService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }
 public  async Task AddActionAsync(User user   , string act)
    {
if (!user.actions.Contains(act))
{
    user.actions.Add(act);
}

var action  = await _context.UserActions.FirstOrDefaultAsync(x=>x.Action==act);
        if (action == null)
        {
             action = new UserAction()
            {
                Action = act
            };
            await _context.UserActions.AddAsync(action);
                    await _context.SaveChangesAsync(); 
            
        }
var UserAction =new UserActionHistory(){
            users_id  =user.Id,
            actions_id = action!.Id
        };

await _context.UserActionHistories.AddAsync(UserAction);
await _context.SaveChangesAsync(); 
_cache.Remove(CacheKeys.UserHistory(user.Id));
_logger.LogInformation("Р”РѕР±Р°РІР»РµРЅР° РёСЃС‚РѕСЂРёСЏ РґРµР№СЃС‚РІРёСЏ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ. РРґРµРЅС‚РёС„РёРєР°С‚РѕСЂ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ: {UserId}, РґРµР№СЃС‚РІРёРµ: {Action}", user.Id, act);

        

    }
}

