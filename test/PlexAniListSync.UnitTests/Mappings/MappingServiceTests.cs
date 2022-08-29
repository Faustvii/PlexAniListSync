using System.Collections.Generic;
using Moq;
using PlexAniListSync.Models.Mappings;
using PlexAniListSync.Services.Caching;
using PlexAniListSync.Services.Mappings;
using Xunit;

namespace PlexAniListSync.UnitTests.Mappings;

public class MappingServiceTests
{
    [Theory]
    [MemberData(nameof(MappingServiceTestData.GetSeasonTestData), MemberType = typeof(MappingServiceTestData))]
    public void Can_Find_Correct_Seasons_From_Season_And_Episode(int expectedId, int seasonNumber, int episode, AnilistSeason[] seasons)
    {
        var dataCache = new Mock<IDataCache>();
        dataCache.Setup(x => x.GetAnilistMapping()).Returns(new List<AnilistMapping>() {
            new AnilistMapping
            {
                Title = "MyTitle",
                Seasons = seasons
            }
        });
        var service = new MappingService(dataCache.Object);
        var id = service.GetAniListIdFromTitle("MyTitle", seasonNumber, episode);
        Assert.Equal(expectedId, id);
    }

    [Theory]
    [InlineData(1, "86: Eighty Six", "86: Eighty Six", new[] { "86", "Eighty Six" })]
    [InlineData(1, "Eighty Six", "86: Eighty Six", new[] { "86", "Eighty Six" })]
    [InlineData(1, "86", "86: Eighty Six", new[] { "86", "Eighty Six" })]
    [InlineData(0, "87", "86: Eighty Six", new[] { "86", "Eighty Six" })]
    [InlineData(0, "Six", "86: Eighty Six", new[] { "86", "Eighty Six" })]
    [InlineData(0, "Eighty", "86: Eighty Six", new[] { "86", "Eighty Six" })]
    // [MemberData(nameof(MappingServiceTestData.GetSeasonTestData), MemberType = typeof(MappingServiceTestData))]
    public void Can_Find_Show_From_Title_Or_Synonyms(int expectedId, string query, string title, string[] synonyms)
    {
        var dataCache = new Mock<IDataCache>();
        dataCache.Setup(x => x.GetAnilistMapping()).Returns(new List<AnilistMapping>() {
            new AnilistMapping
            {
                Title = title,
                Synonyms = synonyms,
                Seasons = new[] { new AnilistSeason { AnilistId = expectedId, Number = 1 } }
            }
        });
        var service = new MappingService(dataCache.Object);
        var id = service.GetAniListIdFromTitle(query, 1, 1);
        Assert.Equal(expectedId, id);
    }

    public static class MappingServiceTestData
    {
        private static readonly AnilistSeason[] MultipleSeasonData = new[]
        {
                new AnilistSeason{ Number = 1, AnilistId = 100},
                new AnilistSeason{ Number = 1, AnilistId = 101, Start = 12},
                new AnilistSeason{ Number = 2, AnilistId = 102}
        };

        private static readonly AnilistSeason[] SingleSeasonData = new[]
        {
                new AnilistSeason{ Number = 1, AnilistId = 104}
        };

        public static IEnumerable<object[]> GetSeasonTestData()
        {
            yield return s_testOne;
            yield return s_testTwo;
            yield return s_testThree;
            yield return s_testFour;
            yield return s_testFive;
        }

        private static readonly object[] s_testOne = new object[]
        {
            100,
            1,
            1,
            MultipleSeasonData
        };

        private static readonly object[] s_testTwo = new object[]
        {
            100,
            1,
            11,
            MultipleSeasonData
        };

        private static readonly object[] s_testThree = new object[]
        {
            101,
            1,
            12,
            MultipleSeasonData
        };

        private static readonly object[] s_testFour = new object[]
        {
            102,
            2,
            12,
            MultipleSeasonData
        };

        private static readonly object[] s_testFive = new object[]
        {
            104,
            1,
            12,
            SingleSeasonData
        };
    }
}
