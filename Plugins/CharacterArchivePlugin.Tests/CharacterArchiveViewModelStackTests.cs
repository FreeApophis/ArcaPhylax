using CharacterArchivePlugin.Data;
using CharacterArchivePlugin.Tests.Fakes;
using CharacterArchivePlugin.ViewModels;
using Xunit;

namespace CharacterArchivePlugin.Tests;

public sealed class CharacterArchiveViewModelStackTests
{
    [Fact]
    public async Task RunSearch_WhenRowsShareImageHash_ProducesOneTileWithVariantCount()
    {
        // Arrange
        var repository = new FakeCharacterRepository();
        repository.Stacks.Add(new CharacterStack("chub:1", "chub", "Alice", "hashA", 3));
        using var viewModel = new CharacterArchiveViewModel(repository, new FakeImageStore());

        // Act
        await viewModel.SearchCommand.ExecuteAsync(null);

        // Assert
        var tile = Assert.Single(viewModel.Results);
        Assert.Multiple(
            () => Assert.Equal(3, tile.VariantCount),
            () => Assert.True(tile.IsStack),
            () => Assert.Equal("×3", tile.CountLabel));
    }

    [Fact]
    public async Task CountLabel_WhenSingleRow_TileIsNotAStack()
    {
        // Arrange
        var repository = new FakeCharacterRepository();
        repository.Stacks.Add(new CharacterStack("chub:1", "chub", "Alice", "hashA", 1));
        using var viewModel = new CharacterArchiveViewModel(repository, new FakeImageStore());

        // Act
        await viewModel.SearchCommand.ExecuteAsync(null);

        // Assert
        var tile = Assert.Single(viewModel.Results);
        Assert.False(tile.IsStack);
    }

    [Fact]
    public async Task SelectTile_WhenStackSelected_LoadsAllVariantsAndSelectsRepresentative()
    {
        // Arrange
        var repository = StackedRepository();
        using var viewModel = new CharacterArchiveViewModel(repository, new FakeImageStore());
        await viewModel.SearchCommand.ExecuteAsync(null);

        // Act
        viewModel.SelectedTile = Assert.Single(viewModel.Results);
        await viewModel.Selection;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(2, viewModel.Variants.Count),
            () => Assert.True(viewModel.HasVariants),
            () => Assert.Equal("chub:1", viewModel.SelectedVariant?.Key),
            () => Assert.Equal("chub:1", viewModel.SelectedDetail?.Key));
    }

    [Fact]
    public async Task SelectVariant_WhenChanged_LoadsThatVariantsDetail()
    {
        // Arrange
        var repository = StackedRepository();
        using var viewModel = new CharacterArchiveViewModel(repository, new FakeImageStore());
        await viewModel.SearchCommand.ExecuteAsync(null);
        viewModel.SelectedTile = Assert.Single(viewModel.Results);
        await viewModel.Selection;
        var other = viewModel.Variants.First(variant => variant.Key == "booru:9");

        // Act
        viewModel.SelectedVariant = other;
        await viewModel.Selection;

        // Assert
        Assert.Equal("booru:9", viewModel.SelectedDetail?.Key);
    }

    private static FakeCharacterRepository StackedRepository()
    {
        var repository = new FakeCharacterRepository();
        repository.Stacks.Add(new CharacterStack("chub:1", "chub", "Alice", "hashA", 2));
        repository.Variants.Add(new CharacterTile("chub:1", "chub", "Alice", "hashA"));
        repository.Variants.Add(new CharacterTile("booru:9", "booru", "Alice v2", "hashA"));
        repository.Details["chub:1"] = new Character("chub:1", "chub", "Alice", "author", "summary", "desc", "hashA", []);
        repository.Details["booru:9"] = new Character("booru:9", "booru", "Alice v2", "author", "summary", "desc", "hashA", []);
        return repository;
    }
}
