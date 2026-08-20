using ArcaPhylax.CharacterCards.Cards;
using Xunit;

namespace ArcaPhylax.CharacterCards.Tests;

public sealed class CardVersionTests
{
    [Fact]
    public void Next_WhenVersionIsBlank_StartsAtOneDotZeroRevisionOne()
    {
        // Act
        var next = CardVersion.Next(current: string.Empty, previous: string.Empty);

        // Assert
        Assert.Equal("1.0 r1", next);
    }

    [Fact]
    public void Next_WhenVersionUnchangedWithRevision_IncrementsRevision()
    {
        // Act
        var next = CardVersion.Next(current: "1.0 r1", previous: "1.0 r1");

        // Assert
        Assert.Equal("1.0 r2", next);
    }

    [Fact]
    public void Next_WhenVersionUnchangedAndRevisionMissing_AddsRevisionOne()
    {
        // Act
        var next = CardVersion.Next(current: "2.5", previous: "2.5");

        // Assert
        Assert.Equal("2.5 r1", next);
    }

    [Fact]
    public void Next_WhenRevisionReachesTwoDigits_IncrementsNumerically()
    {
        // Act
        var next = CardVersion.Next(current: "1.0 r9", previous: "1.0 r9");

        // Assert
        Assert.Equal("1.0 r10", next);
    }

    [Fact]
    public void Next_WhenUserRaisedRevisionButVersionUnchanged_IncrementsFromTypedRevision()
    {
        // Act
        var next = CardVersion.Next(current: "1.0 r10", previous: "1.0 r3");

        // Assert
        Assert.Equal("1.0 r11", next);
    }

    [Fact]
    public void Next_WhenUserChangedVersion_ResetsRevisionToOne()
    {
        // Act
        var next = CardVersion.Next(current: "2.0", previous: "1.0 r3");

        // Assert
        Assert.Equal("2.0 r1", next);
    }

    [Fact]
    public void Next_WhenUserChangedVersionAndTypedRevision_StillResetsToOne()
    {
        // Act
        var next = CardVersion.Next(current: "2.0 r5", previous: "1.0 r3");

        // Assert
        Assert.Equal("2.0 r1", next);
    }

    [Fact]
    public void Next_WhenPreviousBlankAndCurrentHasVersion_StartsRevisionAtOne()
    {
        // Act
        var next = CardVersion.Next(current: "1.0", previous: string.Empty);

        // Assert
        Assert.Equal("1.0 r1", next);
    }

    [Fact]
    public void Next_WhenVersionHasSurroundingWhitespace_TrimsBeforeComparing()
    {
        // Act
        var next = CardVersion.Next(current: "  1.0 r2  ", previous: "1.0 r2");

        // Assert
        Assert.Equal("1.0 r3", next);
    }
}
