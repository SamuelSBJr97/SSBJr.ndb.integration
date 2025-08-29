using SSBJr.ndb.integration.Web.Data;
using Microsoft.EntityFrameworkCore;
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
    private readonly ApiManagerDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        ApiManagerDbContext context,
        ILogger<AuditService> logger, 
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
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

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit: {Action} - {Description} by {UserId} at {Timestamp}", 
                action, description, userId, auditLog.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry for action {Action}", action);
        }
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(int page = 1, int pageSize = 50, string? entityId = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(entityId))
                query = query.Where(log => log.EntityId == entityId);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(log => log.UserId.Equals(userId));

            if (fromDate.HasValue)
                query = query.Where(log => log.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(log => log.Timestamp <= toDate.Value);

            return await query
                .OrderByDescending(log => log.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return new List<AuditLog>();
        }
    }

    public async Task<Dictionary<string, object>> GetAuditStatisticsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var totalLogs = await _context.AuditLogs.CountAsync();
            var logsToday = await _context.AuditLogs.CountAsync(l => l.Timestamp.Date == now.Date);
            var logsThisWeek = await _context.AuditLogs.CountAsync(l => l.Timestamp >= now.AddDays(-7));
            var uniqueUsers = await _context.AuditLogs.Select(l => l.UserId).Distinct().CountAsync();

            var severityBreakdown = await _context.AuditLogs
                .GroupBy(l => l.Severity)
                .ToDictionaryAsync(g => g.Key.ToString(), g => g.Count());

            var actionBreakdown = await _context.AuditLogs
                .GroupBy(l => l.Action)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var stats = new Dictionary<string, object>
            {
                ["total_logs"] = totalLogs,
                ["logs_today"] = logsToday,
                ["logs_this_week"] = logsThisWeek,
                ["unique_users"] = uniqueUsers,
                ["severity_breakdown"] = severityBreakdown,
                ["action_breakdown"] = actionBreakdown
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit statistics");
            return new Dictionary<string, object>();
        }
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
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditArchiveService> _logger;
    private readonly IConfiguration _configuration;

    public AuditArchiveService(
        IServiceProvider serviceProvider,
        ILogger<AuditArchiveService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = _configuration.GetValue<int>("AuditArchive:IntervalHours", 24);
        var archiveOlderThanDays = _configuration.GetValue<int>("AuditArchive:ArchiveOlderThanDays", 90);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Audit archive service running");
                
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApiManagerDbContext>();

                var cutoffDate = DateTime.UtcNow.AddDays(-archiveOlderThanDays);
                var logsToArchive = await context.AuditLogs
                    .Where(log => log.Timestamp < cutoffDate)
                    .CountAsync(stoppingToken);

                if (logsToArchive > 0)
                {
                    _logger.LogInformation("Found {Count} audit logs to archive", logsToArchive);
                    
                    // Here you could implement actual archiving logic
                    // For now, we'll just log the count
                }

                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audit archive service");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // Wait 30 min on error
            }
        }
    }
}