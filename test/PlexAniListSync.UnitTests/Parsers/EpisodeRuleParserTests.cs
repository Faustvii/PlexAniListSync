using System.Collections.Generic;
using System.IO;
using PlexAniListSync.Services.Parsers;
using Xunit;

namespace PlexAniListSync.UnitTests.Parsers;

public class EpisodeRuleParserTests
{
    [Theory]
    [MemberData(nameof(GetFileTestData))]
    [InlineData(1, "::rules\n\n- 41380|43367|116242:13-24 -> 44881|43883|127366:1-12!")]

    public void MultipleRulesCanBeParsed(long ruleCount, string content)
    {
        var service = new EpisodeRuleParser();
        var rules = service.ParseRules(content);
        Assert.Equal(ruleCount, rules.Count);
    }

    [Theory]
    [InlineData(true, "15061", new[] { 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 }, "::rules\n ?|7205|15061:20-30 -> ?|9096|20794:1-10")]
    [InlineData(true, "132528", new[] { 13 }, "::rules\n 48775|44420|132528:13-? -> 50685|45699|143150:1-?!")]
    [InlineData(true, "16009", new[] { 13 }, "::rules\n 16009|7358|16009:13 -> 20423|7989|20423:1")]
    [InlineData(false, "20423", new[] { 1 }, "::rules\n 16009|7358|16009:13 -> 20423|7989|20423:1")]
    [InlineData(true, "21459", new[] { 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63 }, "::rules\n 31964|11469|21459:39-63 -> 36456|13881|100166:1-25!")]
    [InlineData(false, "100166", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 }, "::rules\n 31964|11469|21459:39-63 -> 36456|13881|100166:1-25!")]
    public void RuleIsParsedCorrectly(bool fromMapping, string anilistId, int[] episodeRange, string content)
    {
        var service = new EpisodeRuleParser();
        var rules = service.ParseRules(content);
        var rule = rules[0];
        var mapping = fromMapping ? rule.From : rule.To;
        Assert.Equal(anilistId, mapping.AnilistId);
        Assert.Equal(episodeRange.Length, mapping.EpisodeRange.Length);
        Assert.Equal(episodeRange, mapping.EpisodeRange);
    }

    public static IEnumerable<object[]> GetFileTestData()
    {
        var stringContent = File.ReadAllText("Parsers/rules.test.data");
        yield return new object[] { 56, stringContent };
    }
}
