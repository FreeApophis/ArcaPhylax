using ArcaPhylax.Data;
using Xunit;

namespace ArcaPhylax.Infrastructure.Tests;

public sealed class DropSourceColumnsTests
{
    [Fact]
    public void Of_GivenLinkedFolder_ReturnsFolderKindWithOriginalPath()
    {
        // Arrange
        var source = new LinkedFolder(@"C:\docs");
        var expected = new DropSourceColumns("Folder", @"C:\docs", null);

        // Act
        var columns = DropSourceColumns.Of(source);

        // Assert
        Assert.Equal(expected, columns);
    }

    [Fact]
    public void Of_GivenLinkedFile_ReturnsFileKindWithOriginalPath()
    {
        // Arrange
        var source = new LinkedFile(@"C:\docs\a.pdf");
        var expected = new DropSourceColumns("File", @"C:\docs\a.pdf", null);

        // Act
        var columns = DropSourceColumns.Of(source);

        // Assert
        Assert.Equal(expected, columns);
    }

    [Fact]
    public void Of_GivenCachedFile_ReturnsFileKindWithStoredPath()
    {
        // Arrange
        var source = new CachedFile(@"C:\app\Dropped\a.pdf");
        var expected = new DropSourceColumns("File", null, @"C:\app\Dropped\a.pdf");

        // Act
        var columns = DropSourceColumns.Of(source);

        // Assert
        Assert.Equal(expected, columns);
    }

    public static TheoryData<DropSource> AllSources() =>
    [
        new LinkedFolder(@"C:\docs"),
        new LinkedFile(@"C:\docs\a.pdf"),
        new CachedFile(@"C:\app\Dropped\a.pdf"),
    ];

    [Theory]
    [MemberData(nameof(AllSources))]
    public void ToDropSource_GivenColumnsOfASource_RoundTripsToThatSource(DropSource source)
    {
        // Arrange
        var columns = DropSourceColumns.Of(source);

        // Act
        var roundTripped = columns.ToDropSource();

        // Assert
        Assert.Some(source, roundTripped);
    }

    [Theory]
    [InlineData("Folder", null, null)]
    [InlineData("Folder", null, @"C:\app\Dropped\x")]
    [InlineData("File", null, null)]
    [InlineData("File", @"C:\docs\a.pdf", @"C:\app\Dropped\a.pdf")]
    [InlineData("Url", "https://example.org", null)]
    public void ToDropSource_GivenInvalidCombination_ReturnsNone(string kind, string? originalPath, string? storedPath)
    {
        // Arrange
        var columns = new DropSourceColumns(kind, originalPath, storedPath);

        // Act
        var source = columns.ToDropSource();

        // Assert
        Assert.None(source);
    }
}
