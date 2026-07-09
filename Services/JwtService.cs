
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using MyApiBlya.Services;

public class JwtService : IJwtTokenService
{
    private readonly string secretKey;

    public JwtService(IConfiguration configuration)
    {
        secretKey = configuration["JWT_KEY"]
            ?? configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key не настроен.");
    }

    public  Task<string> GenerateUserTokenAsync(User user)
    {
            var claims= new List<Claim> //claims создают в токене подтверждение данных о пользователя
            {
               
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.Login??""),
                new Claim(ClaimTypes.Role,"User")
                
            };
           

            var key  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));//переводим строку секретного ключа в байты,чтобы сработал алгоритм подписи 
            var creditinals = new SigningCredentials(key,SecurityAlgorithms.HmacSha256); //прописываем алгоритм шифрования секретного ключа


var token = new JwtSecurityToken( /// объект токена,его содержимое,но пока не сам токен
    audience:"MyClients",
    claims: claims,
    expires: DateTime.UtcNow.AddMinutes(3),
    signingCredentials: creditinals
);
var handler = new JwtSecurityTokenHandler().WriteToken(token); ///через конструктор создаем объект-обработчик токена и вызываем метод,превращающий токен в строку для пользователя  
return Task.FromResult(handler);

    } 
      
    

    public  Task<string> GenerateAdminTokenAsync(User user)
    {
            var claims= new List<Claim> //claims создают в токене подтверждение данных о пользователя
            {
               
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.Login??""),
                new Claim(ClaimTypes.Role,"Admin")
                
            };
            
            var key  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));//переводим строку секретного ключа в байты,чтобы сработал алгоритм подписи 
            var creditinals = new SigningCredentials(key,SecurityAlgorithms.HmacSha256); //прописываем алгоритм шифрования секретного ключа


var token = new JwtSecurityToken( /// объект токена,его содержимое,но пока не сам токен
    audience:"MyClients",
    claims: claims,
    expires: DateTime.UtcNow.AddMinutes(3),
    signingCredentials: creditinals
);
var handler = new JwtSecurityTokenHandler().WriteToken(token); ///через конструктор создаем объект-обработчик токена и вызываем метод,превращающий токен в строку для пользователя  
return  Task.FromResult(handler);

    } 
}


