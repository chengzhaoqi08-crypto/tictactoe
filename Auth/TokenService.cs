using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TicTacToe.Data;

namespace TicTacToe.Auth;

/// <summary>Issues signed JWTs carrying the user's id and username.</summary>
public class TokenService
{
    private readonly SymmetricSecurityKey _key;

    public TokenService(byte[] key) => _key = new SymmetricSecurityKey(key);

    public string Create(User user)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("username", user.Username),
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
