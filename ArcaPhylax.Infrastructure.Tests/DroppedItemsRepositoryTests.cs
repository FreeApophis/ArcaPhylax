using ArcaPhylax.Data;
using ArcaPhylax.Infrastructure.Tests.Fakes;
using Xunit;

namespace ArcaPhylax.Infrastructure.Tests;

public sealed class DroppedItemsRepositoryTests
{
    public static TheoryData<DropSource> AllSources() =>
    [
        new LinkedFolder(@"C:\docs"),
        new LinkedFile(@"C:\docs\a.pdf"),
        new CachedFile(@"C:\app\Dropped\a.pdf"),
    ];

    [Theory]
    [MemberData(nameof(AllSources))]
    public void Latest_AfterAdd_ReturnsItemWithTheAddedSource(DropSource source)
    {
        // Arrange
        using var db = new TempDatabase();
        db.Runner().RunCore();
        var sut = new DroppedItemsRepository(db.Factory);
        sut.Add("a.pdf", source);

        // Act
        var item = Assert.Single(sut.Latest(limit: 10));

        // Assert
        Assert.Equal(source, item.Source);
    }

    [Fact]
    public void Latest_GivenRowWithInvalidColumnCombination_SkipsThatRow()
    {
        // Arrange
        using var db = new TempDatabase();
        db.Runner().RunCore();
        db.Scalar("""
            INSERT INTO DroppedItems (Kind, Name, OriginalPath, StoredPath, CreatedUtc)
            VALUES ('Folder', 'no-path', NULL, NULL, '2026-01-01T00:00:00Z');
            """);
        var sut = new DroppedItemsRepository(db.Factory);
        sut.Add("a.pdf", new LinkedFile(@"C:\docs\a.pdf"));

        // Act
        var names = sut.Latest(limit: 10).Select(item => item.Name);

        // Assert
        Assert.Equal(["a.pdf"], names);
    }
}
