using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FileProcessing.API.Configurations;

namespace FileProcessing.API.Services;

public class TokenService : ITokenService
{
    private readonly TokenOptions _opts;
    private readonly byte[] _key;

    public TokenService(IOptions<TokenOptions> opts)
    {
        _opts = opts.Value;
        _key = Encoding.UTF8.GetBytes(_opts.Key);
    }

    public string GenerateToken(string subject, IEnumerable<Claim>? extraClaims = null)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, subject)
        };

        if (extraClaims != null) claims.AddRange(extraClaims);

        var creds = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opts.ExpiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
