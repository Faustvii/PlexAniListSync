using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using PlexAniListSync.Models.Webhook;
using Xunit;

namespace PlexAniListSync.IntegrationTests.API;

public class APITests
{
    [Fact]
    public async Task CanRetrieveWebhookConfigFromAPI()
    {
        var aniListClientMock = new Mock<AniListNet.AniClient>(MockBehavior.Loose);
        await using var appFactory = new CustomWebApplicationFactory<Program>(services =>
        {
            services.AddSingleton(aniListClientMock.Object);
        });

        var client = appFactory.CreateClient();

        var response = await client.GetAsync("/Webhook/Config");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
        Assert.Contains("plex", content);
    }

    [Fact]
    public async Task CanPostWebhookEventToAPI()
    {
        // 1. Mock HttpMessageHandler to intercept HTTP calls
        var responseObject = new
        {
            data = new
            {
                Page = new
                {
                    pageInfo = new { total = 1, perPage = 20, currentPage = 1, lastPage = 1, hasNextPage = false },
                    media = new[]
                    {
                        new
                        {
                            id = 108241,
                            idMal = 39463,
                            title = new
                            {
                                romaji = "Gleipnir",
                                english = "Gleipnir",
                                native = "グレイプニル",
                                userPreferred = "Gleipnir"
                            },
                            type = "ANIME",
                            format = "TV",
                            status = "FINISHED",
                            description = "Blabla",
                            descriptionHtml = "<p>blabla</p>",
                            startDate = new { year = 2020, month = 4, day = 5 },
                            endDate = new { year = 2020, month = 6, day = 28 },
                            season = "SPRING",
                            seasonYear = 2020,
                            episodes = 13,
                            chapters = (int?)null,
                            volumes = (int?)null,
                            source = "MANGA",
                            coverImage = new { color = "#e4861a" },
                            bannerImage = "108241-Cbfv8GHRMFoQ.jpg",
                            genres = new[] { "Action", "Ecchi", "Mystery", "Supernatural" },
                            synonyms = new[] { "格莱普尼尔" },
                            averageScore = 66,
                            meanScore = 66,
                            popularity = 130729,
                            favourites = 1931,
                            isAdult = false,
                            isLicensed = true,
                            siteUrl = "https://anilist.co/anime/108241",
                            isFavourite = false,
                            mediaListEntry = (object?)null,
                            duration = 24,
                            updatedAt = 1750277761
                        }
                    }
                }
            }
        };
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(responseObject)),
                }
            );

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://localhost/anilist/api") };

        // 2. Create AniClient and inject mock HttpClient via reflection
        var aniClient = new AniListNet.AniClient();
        var clientField = typeof(AniListNet.AniClient).GetField(
            "_client",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        if (clientField == null)
            throw new Exception("Could not find _client field on AniClient.");
        clientField.SetValue(aniClient, httpClient);

        await using var appFactory = new CustomWebApplicationFactory<Program>(services =>
        {
            var originalClient = services.SingleOrDefault(
                service => service.ServiceType == typeof(AniListNet.AniClient)
            );
            if (originalClient != null)
                services.Remove(originalClient);
            services.AddSingleton(aniClient);
        });

        var client = appFactory.CreateClient();

        var webhookData = new WebhookData
        {
            Episode = 1,
            Season = 1,
            ShowTitle = "Gleipnir",
            User = "myFirstUser",
            Type = MediaType.Show,
            PlexGuid = "plex://show/12345",
            EpisodeRatingKey = "plex://episode/67890",
            SeasonRatingKey = "plex://season/54321",
            ShowRatingKey = "plex://show/12345"
        };

        var response = await client.PostAsJsonAsync("/Webhook", webhookData);

        response.EnsureSuccessStatusCode();
        handlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
    }
}
