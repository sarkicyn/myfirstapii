using System; 
using MyApiBlya.Services; 
public class History
{
    public int Id{get;set;}
    public string? Action{get;set;}
    
    public List<UsersHistory> users {get;set;} = new();
}