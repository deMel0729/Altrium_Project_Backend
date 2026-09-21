// written by malan
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Altrium_Project_Backend.Models;
using Microsoft.IdentityModel.Tokens;

namespace Altrium_Project_Backend.Security
{
    // Bound from the "Jwt" section of configuration. In production these come from
    // App Service application settings, never from appsettings.json in git.
    public class JwtOptions
    {
        public const string Section = "Jwt";

        public string Issuer { get; set; } = "altrium-crm-api";
        public string Audience { get; set; } = "altrium-crm-client";
        public string Key { get; set; } = string.Empty;      // signing secret, >= 32 chars
        public int ExpiryHours { get; set; } = 8;            // one working day
    }

    public interface IJwtTokenService
    {
        (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _options;
        public JwtTokenService(JwtOptions options) => _options = options;

        public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(User user)
        {
            var expires = DateTime.UtcNow.AddHours(_options.ExpiryHours);

            // These claims are what every later request is authorised against. They are
            // signed, so the client can read them but cannot change them.
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Role, user.UserRole),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
