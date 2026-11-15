using Xunit;

namespace ArcaPhylax.PluginPackaging.Tests;

public sealed class ContentListingTests
{
    [Fact]
    public void Of_GivenEntriesInAnyOrder_ListsThemOrderedByPathOnePerLine()
    {
        // Arrange
        var entries = new[]
        {
            new ContentEntry("payload/b.dll", "bb"),
            new ContentEntry("plugin.json", "pp"),
            new ContentEntry("payload/a.dll", "aa"),
        };
        const string expected = "aa payload/a.dll\nbb payload/b.dll\npp plugin.json\n";

        // Act
        var listing = ContentListing.Of(entries);

        // Assert
        Assert.Equal(expected, listing);
    }

    [Fact]
    public void Of_OrdersPathsOrdinallyNotCultureAware()
    {
        // Arrange
        var entries = new[] { new ContentEntry("payload/a.dll", "1"), new ContentEntry("payload/B.dll", "2") };
        const string expected = "2 payload/B.dll\n1 payload/a.dll\n";

        // Act
        var listing = ContentListing.Of(entries);

        // Assert
        Assert.Equal(expected, listing);
    }
}
