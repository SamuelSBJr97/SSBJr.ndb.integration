using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;

    public CustomAuthenticationStateProvider(
        IJSRuntime jsRuntime,
        HttpClient httpClient,
        ILogger<CustomAuthenticationStateProvider> logger)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "accessToken");
            
            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Validate token with server
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("/api/auth/me");
            
            if (response.IsSuccessStatusCode)
            {
                var userJson = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<UserDto>(userJson, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new(ClaimTypes.Name, user.Username),
                        new(ClaimTypes.Email, user.Email),
                        new(ClaimTypes.Role, user.Role),
                        new("FullName", user.FullName)
                    };

                    // Add permissions as claims
                    claims.AddRange(user.Permissions.Select(p => new Claim("Permission", p)));

                    var identity = new ClaimsIdentity(claims, "jwt");
                    return new AuthenticationState(new ClaimsPrincipal(identity));
                }
            }
            else
            {
                // Token is invalid, try to refresh
                var refreshToken = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "refreshToken");
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshResult = await RefreshTokenAsync(refreshToken);
                    if (refreshResult != null)
                    {
                        return await GetAuthenticationStateAsync(); // Recursive call with new token
                    }
                }

                // Clear invalid tokens
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "accessToken");
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "refreshToken");
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AuthResult>(resultJson, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });

                if (result?.Success == true)
                {
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "accessToken", result.AccessToken);
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "refreshToken", result.RefreshToken);
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "user", JsonSerializer.Serialize(result.User));

                    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
        }

        return false;
    }

    public async Task LogoutAsync()
    {
        try
        {
            var refreshToken = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "refreshToken");
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _httpClient.PostAsJsonAsync("/api/auth/logout", new { RefreshToken = refreshToken });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
        finally
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "accessToken");
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "refreshToken");
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "user");

            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }

    private async Task<AuthResult?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });
            
            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AuthResult>(resultJson, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });

                if (result?.Success == true)
                {
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "accessToken", result.AccessToken);
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "refreshToken", result.RefreshToken);
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
        }

        return null;
    }

    public class AuthResult
    {
        public bool Success { get; set; }
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
}