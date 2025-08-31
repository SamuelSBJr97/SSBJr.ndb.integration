using Microsoft.AspNetCore.SignalR;

namespace SSBJr.ndb.integration.Web.ApiService.Services;

public interface INotificationService
{
    Task NotifyApiStatusChange(Guid apiId, string status, string? message = null);
    Task NotifyApiHealthCheck(Guid apiId, bool isHealthy, string? message = null);
    Task NotifyError(string errorMessage, string? details = null);
}

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyApiStatusChange(Guid apiId, string status, string? message = null)
    {
        _logger.LogInformation("API {ApiId} status changed to {Status}", apiId, status);
        
        await _hubContext.Clients.All.SendAsync("InterfaceStatusChanged", new
        {
            ApiId = apiId,
            Status = status,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyApiHealthCheck(Guid apiId, bool isHealthy, string? message = null)
    {
        _logger.LogInformation("API {ApiId} health check: {Status}", apiId, isHealthy ? "Healthy" : "Unhealthy");
        
        await _hubContext.Clients.All.SendAsync("ApiHealthCheck", new
        {
            ApiId = apiId,
            IsHealthy = isHealthy,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyError(string errorMessage, string? details = null)
    {
        _logger.LogError("Error notification: {Message} - {Details}", errorMessage, details);
        
        await _hubContext.Clients.All.SendAsync("Error", new
        {
            Message = errorMessage,
            Details = details,
            Timestamp = DateTime.UtcNow
        });
    }
}