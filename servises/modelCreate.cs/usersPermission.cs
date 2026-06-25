namespace MyApiBlya.Services;

public class usersPerm
{
    public int users_id { get; set; }
    public User? user { get; set; }
    public int perms_id { get; set; }
    public Permissions? permissions { get; set; }
}
