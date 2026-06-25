namespace MyApiBlya.Services;

public class Permissions
{
    public int Id { get; set; }
    public string Permission { get; set; } = "";
    public List<usersPerm> users { get; set; } = new();
}
