using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using MyApiBlya.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public class Refresh : IRefreshing
{
    private readonly AppDbContext _context;
private readonly IJwtCreate _jwt; 
    public Refresh(AppDbContext context,IJwtCreate jwt)
    {
        _context = context;
        _jwt = jwt;
    }

    public   (string RefreshToken, string Hash) GenerateRefreshToken()
    {
        var refreshToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        var hash = Convert.ToBase64String(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)));

        return (refreshToken, hash);
    }

    public async Task SaveRefreshTokenAsync(User user, string hash)
    {
        user.RefreshTokenHash = hash;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);



    }
   
}
