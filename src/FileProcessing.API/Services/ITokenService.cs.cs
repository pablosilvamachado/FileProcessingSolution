using System.Security.Claims;

namespace FileProcessing.API.Services;

public interface ITokenService
{
    string GenerateToken(string subject, IEnumerable<Claim>? extraClaims = null);
}
