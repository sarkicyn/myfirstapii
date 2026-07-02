using BCrypt.Net;
using MyApiBlya.Services;
public class BCryptPasswordHashService:IPasswordHashService
{
    public string HashPassword(LoginDto dto)
    {
        if(dto!=null){
       return BCrypt.Net.BCrypt.HashPassword(dto.password);}
        
        return "пароль отсутствует";
    }
        
}



