using PlexAniListSync.Services.Extractors;
using Xunit;

namespace PlexAniListSync.UnitTests.Extractors;

public class ExtractorTests
{
    [Theory]
    [InlineData("com.plexapp.agents.hama://tvdb-305074/3/41?lang=en", "305074")]
    [InlineData("com.plexapp.agents.hama://tvdb2-331753/0/1?lang=en", "331753")]
    [InlineData("com.plexapp.agents.hama://tvdb3-352408/0/1?lang=en", "352408")]
    [InlineData("com.plexapp.agents.hama://tvdb4-331753/0/1?lang=en", "331753")]
    [InlineData("com.plexapp.agents.hama://tvdb5-331753/0/1?lang=en", "331753")]
    [InlineData("com.plexapp.agents.hama://anidb-15619?lang=en", "15619")]
    [InlineData("com.plexapp.agents.hama://anidb2-15619?lang=en", "15619")]
    [InlineData("com.plexapp.agents.hama://anidb3-15619?lang=en", "15619")]
    [InlineData("com.plexapp.agents.hama://anidb4-15619?lang=en", "15619")]
    public void CanExtractShowIdFromPlexGuid(string input, string expectedId)
    {
        var extractor = new Extractor();
        var showId = extractor.ExtractShowId(input);
        Assert.Equal(expectedId, showId);
    }
}
