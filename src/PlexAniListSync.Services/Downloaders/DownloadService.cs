using Microsoft.Extensions.Logging;

namespace PlexAniListSync.Services.Downloaders;

public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DownloadService> _logger;

    public DownloadService(HttpClient httpClient, ILogger<DownloadService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> DownloadAsync(string url)
    {
        try
        {
            var result = await _httpClient.GetStringAsync(url);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from '{Url}' - Error {ErrorMessage}", url, ex.Message);
        }
        return string.Empty;
    }
}
