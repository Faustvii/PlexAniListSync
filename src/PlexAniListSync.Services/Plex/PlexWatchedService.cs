using Microsoft.Extensions.Options;
using Plex.Api.Factories;
using Plex.ServerApi.Clients.Interfaces;
using PlexAniListSync.Models.Plex;

namespace PlexAniListSync.Services.Plex;

public class PlexWatchedService : IPlexWatchedService
{
    private readonly IPlexFactory _plexFactory;
    private readonly IPlexServerClient _plexServerClient;
    private readonly IPlexLibraryClient _plexLibraryClient;

    public PlexWatchedService(IPlexFactory plexFactory, IPlexServerClient plexServerClient, IPlexLibraryClient plexLibraryClient, IOptions<PlexOptions> options)
    {
        _plexFactory = plexFactory;
        _plexServerClient = plexServerClient;
        _plexLibraryClient = plexLibraryClient;
    }
}
