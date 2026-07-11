using MyApiBlya.Services;

public static class BlockedUserMessage
{
    public static string Create(User user)
    {
        return $"вы были заблокированы админом на по причине {user.Cause},до {user.BlockedUntill}";
    }
}
