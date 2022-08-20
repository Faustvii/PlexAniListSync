using PlexAniListSync.Services.Parsers;
using Xunit;

namespace PlexAniListSync.UnitTests.Parsers
{
    public class EpisodeRuleParserTests
    {
        [Theory]
        [InlineData(50, "# This file includes anime relation data for Taiga. It is used to redirect an\n# episode to another, which is required to handle special episodes and the case\n# where fansub groups use continuous numbering scheme in their releases.\n#\n# Rules are sorted alphabetically by anime title. Rule syntax is:\n#\n#   10001|10002|10003:14-26 -> 20001|20002|20003:1-13!\n#   └─┬─┘ └─┬─┘ └─┬─┘ └─┬─┘    └─┬─┘ └─┬─┘ └─┬─┘ └─┬─┘\n#     1     2     3     4        1     2     3     4\n#\n#   (1) MyAnimeList ID\n#       <https://myanimelist.net/anime/{id}/{title}>\n#   (2) Kitsu ID\n#       <https://kitsu.io/api/edge/anime?filter[text]={title}>\n#   (3) AniList ID\n#       <https://anilist.co/anime/{id}/{title}>\n#   (4) Episode number or range\n#\n#   - \"?\" is used for unknown values.\n#   - \"~\" is used to repeat the source ID.\n#   - \"!\" suffix is shorthand for creating a new rule where destination ID is\n#     redirected to itself.\n#\n# If you are editing this file in the installation directory of Taiga, be aware\n# that it may be overwritten the next time you update the application.\n#\n# The latest version of this file can be found at:\n#   <https://github.com/erengy/anime-relations>\n#\n# This file is in the public domain.\n\n::meta\n\n# Do not change this line.\n- version: 1.3.0\n\n# Update this date when you add, remove or modify a rule.\n- last_modified: 2022-07-19\n\n::rules\n\n# 100-man no Inochi no Ue ni Ore wa Tatteiru -> ~ 2nd Season\n- 41380|43367|116242:13-24 -> 44881|43883|127366:1-12!\n\n# 11eyes -> ~: Momoiro Genmutan\n- 6682|4662|6682:13 -> 7739|5102|7739:1\n\n# 3-gatsu no Lion -> ~ 2nd season\n- 31646|11380|21366:23-44 -> 35180|13401|98478:1-22\n\n# 3D Kanojo: Real Girl -> ~ 2nd Season\n- 36793|14276|100526:13-24 -> 37956|41973|102882:1-12!\n\n# 86 -> ~ 2nd Season\n- 41457|43066|116589:12-23 -> 48569|44398|131586:1-12!\n\n# 91 Days -> ~: Toki no Asase/Subete no Kinou/Ashita, Mata Ashita\n- 32998|11957|21711:13 -> 34777|13598|98778:1\n\n# Acchi Kocchi (TV) -> ~: Place=Princess\n- 12291|6701|12291:13 -> 16273|7401|16273:1\n\n# Aikatsu! -> ~ 2\n- ?|7205|15061:51-101 -> ?|7972|20181:1-51\n# Aikatsu! -> ~ 3\n- ?|7205|15061:102-152 -> ?|9096|20794:1-51\n# Aikatsu! -> ~ 4\n- ?|7205|15061:153-178 -> ?|11142|21307:1-26\n# Aikatsu Stars! -> ~ Hoshi no Tsubasa\n- ?|?|21648:51-100 -> ?|?|98542:1-50\n# Aikatsu Friends! -> ~ Kagayaki no Jewel\n- 37204|41015|101043:51-76 -> 39078|42169|107447:1-26\n\n# Ajin -> ~ 2nd Season\n- 31580|11368|21341:14-26 -> 33253|12115|21799:1-13!\n\n# Ajin OVA -> Ajin 2nd Season OVA\n- 32015|11615|?:3 -> 36625|41634|?:1\n\n# Akagami no Shirayuki-hime -> ~ 2nd Season\n- 30123|10621|21058:13-24 -> 31173|11179|21258:1-12!\n\n# Akuma no Riddle -> ~: Shousha wa Dare? Nukiuchi Test\n- 19429|7844|19429:13 -> 24751|8724|20926:1\n\n# Aldnoah.Zero -> ~ 2nd Season\n- 22729|8297|20632:13-24 -> 27655|9136|20853:1-12\n\n# Amagami SS -> ~: Imouto\n- 8676|5435|8676:26 -> 9925|5943|9925:1\n\n# Amanchu! -> ~ Special\n- 31771|11432|21406:13 -> 33818|12471|98536:1\n\n# Ano Natsu de Matteru -> ~: Bokutachi wa Koukou Saigo no Natsu wo Sugoshinagara, Ano Natsu de Matteiru.\n- 11433|6508|11433:13 -> 23447|8407|20659:1\n\n# Ansatsu Kyoushitsu (TV) -> ~: Deai no Jikan\n- 19759|7973|19759:0 -> 28405|10020|21303:1\n\n# Another -> ~: The Other - Inga\n- 11111|6462|11111:0 -> 11701|6569|11701:1\n\n# Ao Haru Ride -> ~ OVA\n- 21995|8246|20596:0 -> 24151|8488|20837:1\n- 21995|8246|20596:13 -> 24151|8488|20837:2\n\n# Ao Oni The Animation (Movie) -> ~\n- 33820|12478|21898:0 -> ~|~|~:1\n\n# Baccano! -> ~ Specials\n- 2251|2039|2251:14-16 -> 3901|3334|3901:1-3\n\n# Beastars 2nd Season -> ~\n- 40935|42904|114194:13-24 -> ~|~|~:1-12\n\n# Beatless -> ~: Final Stage\n- 36516|13939|?:21-24 -> 38020|41407|?:1-4!\n- 36516|13939|?:25-28 -> 38020|41407|?:1-4!\n- ?|?|100245:25-28 -> ?|?|100245:21-24\n\n# Berserk (2016) -> Berserk (2017)\n- 32379|11655|21560:13-24 -> 34055|12569|97643:1-12!\n\n# Bikini Warriors -> ~ Special\n- 30782|10920|21192:13 -> 31283|11216|21387:1\n# Bikini Warriors -> ~ OVA\n- 30782|10920|21192:14-15 -> 33712|12452|87480:1-2\n\n# Bishoujo Senshi Sailor Moon -> ~ R\n- 530|489|530:47-89 -> 740|664|740:1-43!\n# Bishoujo Senshi Sailor Moon -> ~ S\n- 530|489|530:90-127 -> 532|491|532:1-38!\n# Bishoujo Senshi Sailor Moon -> ~ SuperS\n- 530|489|530:128-166 -> 1239|1115|1239:1-39!\n# Bishoujo Senshi Sailor Moon -> ~: Sailor Stars\n- 530|489|530:167-200 -> 996|886|996:1-34!\n# Bishoujo Senshi Sailor Moon Crystal -> ~ Season III\n- 14751|7163|14751:27-39 -> 31733|11415|21462:1-13!\n\n# Black Lagoon -> ~: The Second Barrage\n- 889|789|889:13-24 -> 1519|1363|1519:1-12!\n\n# Boku no Hero Academia -> ~ 2nd Season\n- 31964|11469|21459:14-38 -> 33486|12268|21856:1-25\n# Boku no Hero Academia 2nd Season -> ~: Hero Note\n- 33486|12268|21856:0 -> 35262|13313|99308:1\n# Boku no Hero Academia -> ~ 3rd Season\n- 31964|11469|21459:39-63 -> 36456|13881|100166:1-25!\n# Boku no Hero Academia -> ~ 4th Season\n- 31964|11469|21459:64-88 -> 38408|41971|104276:1-25!\n# Boku no Hero Academia -> ~ 5th Season\n- 31964|11469|21459:89-113 -> 41587|43108|117193:1-25!\n\n# Boku wa Tomodachi ga Sukunai -> ~ Episode 0\n- 10719|6316|10719:0 -> 10897|6397|10897:1\n\n# Break Blade 1 -> ~ 2,3,4,5,6\n- 6772|4708|6772:2 -> 8514|5366|8514:1\n- 6772|4708|6772:3 -> 9252|5645|9252:1\n- 6772|4708|6772:4 -> 9465|5743|9465:1\n- 6772|4708|6772:5 -> 9724|5837|9724:1\n- 6772|4708|6772:6 -> 10092|6031|10092:1\n\n")]
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
    }
}
