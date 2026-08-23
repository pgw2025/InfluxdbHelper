using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InfluxdbHelper.Api.Dtos;
using InfluxdbHelper.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace InfluxdbHelper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>登录，换取 JWT Token。</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var users = _configuration.GetSection("Auth:Users").Get<List<AuthUser>>() ?? new List<AuthUser>();
            var hash = Sha256Hex(request.Password);

            var user = users.FirstOrDefault(u =>
                string.Equals(u.Username, request.Username, StringComparison.OrdinalIgnoreCase)
                && string.Equals(u.PasswordHash, hash, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                return Ok(ApiResponse.Fail(1001, "用户名或密码错误"));
            }

            var (token, expiresAt) = IssueToken(user);
            return Ok(ApiResponse.Ok(new LoginResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                Username = user.Username,
                DisplayName = user.DisplayName
            }));
        }

        /// <summary>当前登录用户信息。</summary>
        [HttpGet("profile")]
        [Authorize]
        public IActionResult Profile()
        {
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";
            var display = User.FindFirstValue("display_name") ?? username;
            return Ok(ApiResponse.Ok(new UserProfile { Username = username, DisplayName = display }));
        }

        private (string token, DateTime expiresAt) IssueToken(AuthUser user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expireMinutes = _configuration.GetValue<int>("Jwt:ExpireMinutes", 480);
            var expiresAt = DateTime.UtcNow.AddMinutes(expireMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Username),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new("display_name", user.DisplayName)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        private static string Sha256Hex(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private class AuthUser
        {
            public string Username { get; set; } = string.Empty;
            public string PasswordHash { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
        }
    }
}
