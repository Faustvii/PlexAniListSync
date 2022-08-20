namespace PlexAniListSync.Services.Downloaders;

public interface IDownloadService
{
    Task<string> DownloadAsync(string url);
}
