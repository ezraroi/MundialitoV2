using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Mundialito.DAL.Accounts;
using Microsoft.Extensions.Options;
using Mundialito.Configuration;

namespace Mundialito.Auth
{

    public class TokenService
    {
        private const int ExpirationMinutes = 60 * 24 * 60;
        private readonly ILogger<TokenService> _logger;
        private readonly JwtTokenSettings _config;

        public TokenService(ILogger<TokenService> logger, IOptions<JwtTokenSettings> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public string CreateToken(MundialitoUser user)
        {
            var expiration = DateTime.UtcNow.AddMinutes(ExpirationMinutes);
            var token = CreateJwtToken(
                CreateClaims(user),
                CreateSigningCredentials(),
                expiration
            );
            var tokenHandler = new JwtSecurityTokenHandler();
            _logger.LogInformation("JWT Token created");
            return tokenHandler.WriteToken(token);
        }

        private JwtSecurityToken CreateJwtToken(List<Claim> claims, SigningCredentials credentials,
            DateTime expiration) =>
            new(
                _config.ValidIssuer,
                _config.ValidAudience,
                claims,
                expires: expiration,
                signingCredentials: credentials
            );

        private List<Claim> CreateClaims(MundialitoUser user)
        {
            var jwtSub = _config.JwtRegisteredClaimNamesSub;
            _logger.LogInformation("Creating claims for {}", user.UserName);
            try
            {
                var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, jwtSub),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
                // Deliberately no role claim. The token lives for 60 days, so a role baked
                // in here would keep an activated user locked out, and a deactivated user
                // authorized, until it expired. Authorization reads the role from the
                // database instead - see Auth/Authorization/CurrentRoleHandler.
            };
                return claims;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to create claim: {}", e.Message);
                throw;
            }
        }

        private SigningCredentials CreateSigningCredentials()
        {
            var symmetricSecurityKey = _config.SymmetricSecurityKey;
            return new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(symmetricSecurityKey)
                ),
                SecurityAlgorithms.HmacSha256
            );
        }
    }
}