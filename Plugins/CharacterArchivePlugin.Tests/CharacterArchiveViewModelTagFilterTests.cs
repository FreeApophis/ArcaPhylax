using CharacterArchivePlugin.Data;
using CharacterArchivePlugin.Tests.Fakes;
using CharacterArchivePlugin.ViewModels;
using Xunit;

namespace CharacterArchivePlugin.Tests;

public sealed class CharacterArchiveViewModelTagFilterTests
{
    [Fact]
    public async Task AddTag_GivenTag_RunsSearchFilteredByThatTag()
    {
        // Arrange
        var repository = new FakeCharacterRepository();
        using var viewModel = new CharacterArchiveViewModel(repository, new FakeImageStore());

        // Act
        await viewModel.AddTagCommand.ExecuteAsync("fantasy");

        // Assert
        Assert.Equal(new[] { "fantasy" }, repository.LastSearchTags);
    }

    [Fact]
    public async Task AddTag_GivenTagAlreadySelected_DoesNotSelectItTwice()
    {
        // Arrange
        var repository = new FakeCharacterRepository();
        using var viewModel = new CharacterArchiveViewModel(repository, new FakeImageStore());
        await viewModel.AddTagCommand.ExecuteAsync("fantasy");

        // Act
        await viewModel.AddTagCommand.ExecuteAsync("fantasy");

        // Assert
        Assert.Equal(new[] { "fantasy" }, viewModel.SelectedTags);
    }

    [Fact]
    public async Task RemoveTag_GivenSelectedTag_RunsSearchWithoutThatTag()
    {
        // Arrange
        var repository = new FakeCharacterRepository();
        using var viewModel = new CharacterArchiveViewModel(repository, new FakeImageStore());
        await viewModel.AddTagCommand.ExecuteAsync("fantasy");
        await viewModel.AddTagCommand.ExecuteAsync("female");

        // Act
        await viewModel.RemoveTagCommand.ExecuteAsync("fantasy");

        // Assert
        Assert.Equal(new[] { "female" }, repository.LastSearchTags);
    }

    [Fact]
    public async Task AddTag_GivenTagInThePicker_RemovesItFromTheAvailableTags()
    {
        // Arrange
        var repository = new FakeCharacterRepository();
        repository.Tags.Add(new TagOption("fantasy", 10));
        repository.Tags.Add(new TagOption("female", 8));
        using var viewModel = new CharacterArchiveViewModel(repository, new FakeImageStore());
        await viewModel.InitializeAsync();

        // Act
        await viewModel.AddTagCommand.ExecuteAsync("fantasy");

        // Assert
        Assert.DoesNotContain(viewModel.AvailableTags, tag => tag.Tag == "fantasy");
    }

    [Fact]
    public async Task ClearTags_GivenSelectedTags_RunsSearchWithNoTagFilter()
    {
        // Arrange
        var repository = new FakeCharacterRepository();
        using var viewModel = new CharacterArchiveViewModel(repository, new FakeImageStore());
        await viewModel.AddTagCommand.ExecuteAsync("fantasy");

        // Act
        await viewModel.ClearTagsCommand.ExecuteAsync(null);

        // Assert
        Assert.Empty(repository.LastSearchTags!);
    }
}
