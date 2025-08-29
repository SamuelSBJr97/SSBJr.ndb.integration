using SSBJr.ndb.integration.Web.Models;

namespace SSBJr.ndb.integration.Web.Services;

public interface IApiInterfaceService
{
    Task<ApiInterface> CreateAsync(ApiInterfaceCreateRequest request, string userId);
    Task<ApiInterface?> GetByIdAsync(Guid id);
    Task<IEnumerable<ApiInterface>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<ApiInterface> UpdateAsync(Guid id, ApiInterfaceUpdateRequest request, string userId);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeployAsync(Guid id);
    Task<bool> StopAsync(Guid id);
    Task<bool> RestartAsync(Guid id);
    Task<ApiInterface> ValidateAsync(Guid id);
    Task<Dictionary<string, object>> GetMetricsAsync(Guid id);
    Task<List<string>> GetLogsAsync(Guid id, int lines = 100);
}