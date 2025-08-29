using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SSBJr.ndb.integration.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace SSBJr.ndb.integration.Web.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal);
    Task<bool> ValidatePermissionAsync(ClaimsPrincipal principal, string permission);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
}

public class AuthService : IAuthService
{
    private readonly ApiManagerDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IAuditService _auditService;

    public AuthService(
        ApiManagerDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IAuditService auditService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                await _auditService.LogAsync("LOGIN_FAILED", $"Failed login attempt for username: {request.Username}", "anonymous");
                return new AuthResult { Success = false, Message = "Invalid credentials" };
            }

            var (accessToken, refreshToken) = await GenerateTokensAsync(user);
            
            user.LastLoginAt = DateTime.UtcNow;
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            await _auditService.LogAsync("LOGIN_SUCCESS", $"User {user.Username} logged in successfully", user.Id.ToString());

            return new AuthResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Permissions = user.Permissions
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return new AuthResult { Success = false, Message = "Login failed" };
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.IsActive);

            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                return new AuthResult { Success = false, Message = "Invalid or expired refresh token" };
            }

            var (newAccessToken, newRefreshToken) = await GenerateTokensAsync(user);

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return new AuthResult
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Permissions = user.Permissions
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new AuthResult { Success = false, Message = "Token refresh failed" };
        }
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("LOGOUT", $"User {user.Username} logged out", user.Id.ToString());
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return false;
        }
    }

    public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        }
        return null;
    }

    public async Task<bool> ValidatePermissionAsync(ClaimsPrincipal principal, string permission)
    {
        var user = await GetCurrentUserAsync(principal);
        if (user == null) return false;

        // Administrators have all permissions
        if (user.Role == "Administrator") return true;

        return user.Permissions.Contains(permission);
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return new AuthResult { Success = false, Message = "Username already exists" };
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return new AuthResult { Success = false, Message = "Email already exists" };
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                Role = "User", // Default role
                Permissions = new List<string> { "api.read" }, // Default permissions
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("USER_REGISTERED", $"New user registered: {user.Username}", user.Id.ToString());

            var (accessToken, refreshToken) = await GenerateTokensAsync(user);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new AuthResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Permissions = user.Permissions
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username: {Username}", request.Username);
            return new AuthResult { Success = false, Message = "Registration failed" };
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
                return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userGuid && u.IsActive);
            if (user == null)
                return false;

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("PASSWORD_CHANGED", $"Password changed for user: {user.Username}", user.Id.ToString());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return false;
        }
    }

    private async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings.GetValue<string>("SecretKey") ?? "your-super-secret-key-for-jwt-tokens-at-least-256-bits";
        var issuer = jwtSettings.GetValue<string>("Issuer") ?? "SSBJr.ApiManager";
        var audience = jwtSettings.GetValue<string>("Audience") ?? "SSBJr.ApiManager.Users";
        var expiration = jwtSettings.GetValue<int>("ExpirationMinutes", 60);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", user.FullName),
            new Claim("Permissions", string.Join(",", user.Permissions))
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiration),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        return (accessToken, refreshToken);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

// DTOs for authentication
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class AuthResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto? User { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}