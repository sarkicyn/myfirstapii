public static class CacheKeys
{
    public static string UserById(int id) => $"user_{id}";

    public static string UserNotFound(int id) => $"user_{id}_not_found";

    public static string CurrentUserById(int id) => $"current_user_{id}";

    public static string CurrentUserNotFound(int id) => $"current_user_{id}_not_found";

    public static string UserHistory(int id) => $"UserAction{id}";
}

