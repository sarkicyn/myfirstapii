using System;
using MyApiBlya.Services;
public class UsersHistory
{
    public int Id{ get; set; }
    public int users_id{get;set;}
    public int actions_id{get;set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User? user{get;set;}
    public History? history{get;set;}
}
