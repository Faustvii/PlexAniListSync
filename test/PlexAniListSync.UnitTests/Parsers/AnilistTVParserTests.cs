using System.Collections.Generic;
using PlexAniListSync.Models.Mappings;
using PlexAniListSync.Services.Parsers;
using Xunit;

namespace PlexAniListSync.UnitTests.Parsers;

public class AnilistTVParserTests
{
    [Theory]
    [MemberData(nameof(AnilistTVParserTestData.GetTestData), MemberType = typeof(AnilistTVParserTestData))]
    public void CanDeserializeCorrectly(AnilistMapping[] expectedMappings, string yamlContent)
    {
        var parser = new AnilistTVParser();
        var mappings = parser.ParseMappings(yamlContent);
        Assert.Equal(expectedMappings.Length, mappings.Count);
        Assert.Equal(expectedMappings[0].Title, mappings[0].Title);
        Assert.Equal(expectedMappings[0].LookupIds, mappings[0].LookupIds);
        Assert.Equal(expectedMappings[0].Seasons, mappings[0].Seasons);
    }
}

public static class AnilistTVParserTestData
{
    public static IEnumerable<object[]> GetTestData()
    {
        yield return s_testOne;
    }

    private static readonly object[] s_testOne = new object[]
    {
        new[]
        {
            new AnilistMapping
            {
                Title = "86: Eighty Six",
                LookupIds = new[] { "plex://show/5e7362fb4a62f90040e89827" },
                Seasons = new[]
                {
                    new AnilistSeason { Number = 1, AnilistId = 116589 },
                    new AnilistSeason
                    {
                        Number = 1,
                        AnilistId = 131586,
                        Start = 12
                    }
                }
            }
        },
        @"# PlexAniSync: TheTVDB Series Mappings in English
entries:
  - title: ""86: Eighty Six""
    guid: plex://show/5e7362fb4a62f90040e89827
    # imdb: https://www.imdb.com/title/tt13718450/
    # tmdb: https://www.themoviedb.org/tv/100565
    # tvdb: https://www.thetvdb.com/dereferrer/series/378609
    seasons:
      - season: 1
        anilist-id: 116589
      - season: 1
        anilist-id: 131586
        start: 12"
    };
}
