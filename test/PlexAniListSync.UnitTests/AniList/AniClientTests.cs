using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using PlexAniListSync.AniListNet;
using PlexAniListSync.AniListNet.Objects;
using PlexAniListSync.AniListNet.Parameters;
using Xunit;

namespace PlexAniListSync.UnitTests.AniList;

public class AniClientTests
{
    private static AniClient ClientReturning(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);
        return new AniClient(new HttpClient(handler.Object));
    }

    [Fact]
    public async Task Returns429WithHtmlBody_ThrowsTypedRateLimitException_WithoutJsonCrash()
    {
        // Cloudflare sheds load with an HTML 429 page - the exact body that used to crash JSON parsing.
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("<!DOCTYPE html><html><body>429 Too Many Requests</body></html>"),
        };
        response.Headers.TryAddWithoutValidation("Retry-After", "30");
        var client = ClientReturning(response);

        var exception = await Assert.ThrowsAsync<AniListRateLimitException>(
            () => client.SearchMediaAsync(new SearchMediaFilter { Query = "gleipnir" })
        );

        Assert.Equal(30, exception.RetryAfter);
        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.NotNull(exception.RateReset);
    }

    [Fact]
    public async Task SearchMedia_DeserializesGqlSelectionsPrivateSettersAndEnumMembers()
    {
        var payload = new
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
                            format = "TV_SHORT",
                            status = "NOT_YET_RELEASED",
                            startDate = new { year = 2020, month = 4, day = 5 },
                            endDate = new { year = 2020, month = 6, day = 28 },
                            season = "SPRING",
                            seasonYear = 2020,
                            episodes = 13,
                            source = "MANGA",
                            coverImage = new { color = "#e4861a" },
                            bannerImage = "108241-Cbfv8GHRMFoQ.jpg",
                            genres = new[] { "Action", "Ecchi", "Mystery", "Supernatural" },
                            synonyms = new[] { "格莱普尼尔" },
                            siteUrl = "https://anilist.co/anime/108241",
                            duration = 24,
                            updatedAt = 1750277761
                        }
                    }
                }
            }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload)),
        };
        var client = ClientReturning(response);

        var page = await client.SearchMediaAsync(new SearchMediaFilter { Query = "gleipnir" });

        var media = Assert.Single(page.Data);
        Assert.Equal(108241, media.Id);
        Assert.Equal(39463, media.MalId); // [GqlSelection("idMal")] -> renamed property
        Assert.Equal("Gleipnir", media.Title.PreferredTitle); // userPreferred -> PreferredTitle, private setter
        Assert.Equal(MediaType.Anime, media.Type); // [EnumMember] "ANIME"
        Assert.Equal(MediaFormat.TVShort, media.Format); // [EnumMember] "TV_SHORT"
        Assert.Equal(MediaStatus.NotYetReleased, media.Status); // [EnumMember] "NOT_YET_RELEASED"
        Assert.Equal(MediaSeason.Spring, media.Season);
        Assert.Equal(MediaSource.Manga, media.Source);
        Assert.Equal(13, media.Episodes);
        Assert.Equal(new DateTime(2020, 4, 5), media.StartDate.ToDateTime()); // nested Date object
        Assert.Equal(4, media.Genres.Length); // string[] round-trip
        Assert.Contains("格莱普尼尔", media.Synonyms);
    }

    [Fact]
    public async Task SaveMediaEntry_DeserializesEntryProgressStatusAndMaxProgress()
    {
        var payload = new
        {
            data = new
            {
                SaveMediaListEntry = new
                {
                    id = 555,
                    status = "CURRENT",
                    score = 0.0,
                    progress = 7,
                    progressVolumes = (int?)null,
                    startedAt = new { year = 2024, month = 1, day = 2 },
                    completedAt = new { year = (int?)null, month = (int?)null, day = (int?)null },
                    media = new { episodes = 13, chapters = (int?)null, volumes = (int?)null },
                    notes = (string?)null
                }
            }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload)),
        };
        var client = ClientReturning(response);

        var entry = await client.SaveMediaEntryAsync(108241, new MediaEntryMutation { Progress = 7 });

        Assert.Equal(7, entry.Progress);
        Assert.Equal(MediaEntryStatus.Current, entry.Status);
        Assert.Equal(13, entry.MaxProgress); // computed from Media.Episodes
        Assert.Equal(new DateTime(2024, 1, 2), entry.StartDate.ToDateTime());
        Assert.Null(entry.CompleteDate.ToDateTime()); // all-null Date -> null
    }
}
