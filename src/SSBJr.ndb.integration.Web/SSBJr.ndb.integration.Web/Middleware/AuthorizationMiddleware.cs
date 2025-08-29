using System.Security.Claims;
using SSBJr.ndb.integration.Web.Services;

namespace SSBJr.ndb.integration.Web.Middleware;

public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationMiddleware> _logger;

    public AuthorizationMiddleware(RequestDelegate next, ILogger<AuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        // Skip authorization for certain paths
        var path = context.Request.Path.Value?.ToLower();
        if (ShouldSkipAuthorization(path))
        {
            await _next(context);
            return;
        }

        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        // Check permissions for API endpoints
        if (path?.StartsWith("/api/interfaces") == true)
        {
            var method = context.Request.Method.ToUpper();
            var requiredPermission = GetRequiredPermission(method, path);

            if (!string.IsNullOrEmpty(requiredPermission))
            {
                if (!await authService.ValidatePermissionAsync(context.User, requiredPermission))
                {
                    _logger.LogWarning("User {User} denied access to {Path} - missing permission {Permission}",
                        context.User.Identity.Name, path, requiredPermission);

                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Forbidden");
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool ShouldSkipAuthorization(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        var publicPaths = new[]
        {
            "/",
            "/login",
            "/register",
            "/health",
            "/swagger",
            "/api/auth",
            "/_framework",
            "/_blazor",
            "/css",
            "/js",
            "/images",
            "/favicon.ico"
        };

        return publicPaths.Any(p => path.StartsWith(p));
    }

    private static string GetRequiredPermission(string method, string path)
    {
        return method switch
        {
            "GET" when path.Contains("/metrics") || path.Contains("/logs") => "api.monitor",
            "GET" => "api.read",
            "POST" when path.Contains("/deploy") || path.Contains("/stop") || path.Contains("/restart") => "api.deploy",
            "POST" => "api.create",
            "PUT" => "api.update",
            "DELETE" => "api.delete",
            _ => string.Empty
        };
    }
}

// Permission attribute for additional controller-level authorization
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute
{
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
    }
}

// Authorization policy requirements
public static class Permissions
{
    public const string ApiRead = "api.read";
    public const string ApiCreate = "api.create";
    public const string ApiUpdate = "api.update";
    public const string ApiDelete = "api.delete";
    public const string ApiDeploy = "api.deploy";
    public const string ApiMonitor = "api.monitor";
    public const string AuditRead = "audit.read";
    public const string UserManage = "user.manage";
    public const string SystemAdmin = "system.admin";
}

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string ApiManager = "ApiManager";
    public const string Developer = "Developer";
    public const string Viewer = "Viewer";
    public const string User = "User";
}