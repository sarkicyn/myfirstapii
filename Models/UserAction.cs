using System; 
using MyApiBlya.Services; 
public class UserAction
{
    public int Id{get;set;}
    public string? Action{get;set;}
    
    public List<UserActionHistory> users {get;set;} = new();
}
