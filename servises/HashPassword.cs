using BCrypt.Net;
using MyApiBlya.Services;
public class HashPassik:HashPassword
{
    public string HashPass(LoginDTO dto)
    {
        if(dto!=null){
       return BCrypt.Net.BCrypt.HashPassword(dto.password);}
        
        return "пароль отсутствует";
    }
        
}

