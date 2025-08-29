namespace SSBJr.ndb.integration.Web.ApiService.Models;

public class ApiDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SwaggerJson { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public ApiStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHealthCheck { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum ApiStatus
{
    Created,
    Deploying,
    Running,
    Failed,
    Stopped
}

public class ApiDeploymentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SwaggerJson { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class ApiHealthCheck
{
    public Guid ApiId { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime CheckedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
}