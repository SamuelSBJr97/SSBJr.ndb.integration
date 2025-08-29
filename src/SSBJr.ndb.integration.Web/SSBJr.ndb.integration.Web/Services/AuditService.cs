using System.Collections.Concurrent;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.Services;

public interface IAuditService
{
    Task LogAsync(string action, string description, string userId, string? entityId = null, object? additionalData = null);
    Task<IEnumerable<AuditLog>> GetLogsAsync(int page = 1, int pageSize = 50, string? entityId = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<Dictionary<string, object>> GetAuditStatisticsAsync();
}

public class AuditLog
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public string AdditionalData { get; set; } = string.Empty;
    public AuditSeverity Severity { get; set; }
    public string Source { get; set; } = "Web";
}

public enum AuditSeverity
{
    Information,
    Warning,
    Error,
    Critical
}

public class AuditService : IAuditService
{
    private readonly ConcurrentBag<AuditLog> _auditLogs = new();
    private readonly ILogger<AuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ILogger<AuditService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string description, string userId, string? entityId = null, object? additionalData = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                Description = description,
                UserId = userId,
                EntityId = entityId,
                EntityType = DetermineEntityType(action),
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext?.Request.Headers.UserAgent.FirstOrDefault(),
                Timestamp = DateTime.UtcNow,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : string.Empty,
                Severity = DetermineSeverity(action),
                Source = "ApiManager"
            };

            _auditLogs.Add(auditLog);

            _logger.LogInformation("Audit: {Action} - {Description} by {UserId} at {Timestamp}", 
                action, description, userId, auditLog.Timestamp);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry for action {Action}", action);
        }
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(int page = 1, int pageSize = 50, string? entityId = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _auditLogs.AsEnumerable();

        if (!string.IsNullOrEmpty(entityId))
            query = query.Where(log => log.EntityId == entityId);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(log => log.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));

        if (fromDate.HasValue)
            query = query.Where(log => log.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(log => log.Timestamp <= toDate.Value);

        var result = query
            .OrderByDescending(log => log.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return await Task.FromResult(result);
    }

    public async Task<Dictionary<string, object>> GetAuditStatisticsAsync()
    {
        var logs = _auditLogs.ToList();
        var now = DateTime.UtcNow;
        
        var stats = new Dictionary<string, object>
        {
            ["total_logs"] = logs.Count,
            ["logs_today"] = logs.Count(l => l.Timestamp.Date == now.Date),
            ["logs_this_week"] = logs.Count(l => l.Timestamp >= now.AddDays(-7)),
            ["unique_users"] = logs.Select(l => l.UserId).Distinct().Count(),
            ["severity_breakdown"] = logs.GroupBy(l => l.Severity)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            ["action_breakdown"] = logs.GroupBy(l => l.Action)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return await Task.FromResult(stats);
    }

    private string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',').First().Trim();

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string DetermineEntityType(string action)
    {
        return action switch
        {
            var a when a.StartsWith("API_") => "ApiInterface",
            var a when a.StartsWith("USER_") => "User",
            var a when a.StartsWith("SYSTEM_") => "System",
            _ => "Unknown"
        };
    }

    private AuditSeverity DetermineSeverity(string action)
    {
        return action switch
        {
            var a when a.Contains("ERROR") || a.Contains("FAILED") => AuditSeverity.Error,
            var a when a.Contains("DELETE") || a.Contains("REMOVE") => AuditSeverity.Warning,
            var a when a.Contains("SECURITY") || a.Contains("AUTH") => AuditSeverity.Critical,
            _ => AuditSeverity.Information
        };
    }
}

public class AuditArchiveService : BackgroundService
{
    private readonly ILogger<AuditArchiveService> _logger;

    public AuditArchiveService(ILogger<AuditArchiveService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Audit archive service running");
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audit archive service");
            }
        }
    }
}