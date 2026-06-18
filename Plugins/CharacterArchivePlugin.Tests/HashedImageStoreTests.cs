using CharacterArchivePlugin.Services;
using FsCheck.Xunit;
using Funcky.Monads;
using Xunit;

namespace CharacterArchivePlugin.Tests;

public sealed class HashedImageStoreTests
{
    [Fact]
    public void RelativePathForHash_GivenValidMd5_ReturnsShardedPath()
    {
        // Arrange
        var hash = "00004e518046ccd797ae62e4de97608e";
        var expected = Path.Combine("0", "0", "0", "04e518046ccd797ae62e4de97608e");

        // Act
        var result = HashedImageStore.RelativePathForHash(hash);

        // Assert
        Assert.Equal(Option.Some(expected), result);
    }

    [Fact]
    public void RelativePathForHash_GivenUppercaseHash_NormalisesToLowercase()
    {
        // Arrange
        var hash = "ABCDEF00000000000000000000000000";
        var expected = Path.Combine("a", "b", "c", "def00000000000000000000000000");

        // Act
        var result = HashedImageStore.RelativePathForHash(hash);

        // Assert
        Assert.Equal(Option.Some(expected), result);
    }

    [Fact]
    public void RelativePathForHash_GivenHashWithSurroundingWhitespace_Trims()
    {
        // Arrange
        var hash = "  0123456789abcdef0123456789abcdef  ";
        var expected = Path.Combine("0", "1", "2", "3456789abcdef0123456789abcdef");

        // Act
        var result = HashedImageStore.RelativePathForHash(hash);

        // Assert
        Assert.Equal(Option.Some(expected), result);
    }

    [Theory]
    [InlineData("")] // empty
    [InlineData("0123456789abcdef0123456789abcde")] // 31 chars
    [InlineData("0123456789abcdef0123456789abcdef0")] // 33 chars
    [InlineData("0123456789abcdef0123456789abcdeg")] // non-hex 'g'
    public void RelativePathForHash_GivenMalformedHash_ReturnsNone(string hash)
    {
        // Act
        var result = HashedImageStore.RelativePathForHash(hash);

        // Assert
        Assert.Equal(Option<string>.None, result);
    }

    [Fact]
    public void RelativePathForHash_GivenNull_ReturnsNone()
    {
        // Act
        var result = HashedImageStore.RelativePathForHash(null);

        // Assert
        Assert.Equal(Option<string>.None, result);
    }

    [Property]
    public void RelativePathForHash_GivenAnyMd5ShapedHash_RecombinesToTheOriginalHash(Guid guid)
    {
        // Arrange — a Guid formatted as "N" is exactly 32 lowercase hex chars, an MD5-shaped input.
        var hash = guid.ToString("N");

        // Act
        var recombined = HashedImageStore.RelativePathForHash(hash)
            .Match(
                none: () => string.Empty,
                some: path => string.Concat(path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

        // Assert
        Assert.Equal(hash, recombined);
    }
}
