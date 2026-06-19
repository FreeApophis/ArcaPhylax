using DemiurgPlaygroundPlugin.Chat;
using Xunit;

namespace DemiurgPlaygroundPlugin.Tests;

public sealed class TurnClassifierTests
{
    [Theory]
    [InlineData("Computer, freeze program")]
    [InlineData("computer end program")]
    [InlineData("COMPUTER")]
    [InlineData("Arch")]
    [InlineData("arch.")]
    [InlineData("   Computer, status report")]
    public void Classify_GivenLeadingWakeWord_IsAddressedToComputer(string input)
    {
        var result = TurnClassifier.Classify(input);

        Assert.True(result.AddressedToComputer);
    }

    [Theory]
    [InlineData("I draw my sword and charge")]
    [InlineData("Computers are everywhere in this room")]
    [InlineData("The archway looms ahead")]
    [InlineData("Tell the computer to wait")]
    public void Classify_GivenNonWakeWordOpening_IsNotAddressedToComputer(string input)
    {
        var result = TurnClassifier.Classify(input);

        Assert.False(result.AddressedToComputer);
    }

    [Fact]
    public void Classify_TrimsSurroundingWhitespaceInText()
    {
        var result = TurnClassifier.Classify("   hello there   ");

        Assert.Equal("hello there", result.Text);
    }

    [Fact]
    public void MarkedText_WhenAddressedToComputer_UsesComputerMarker()
    {
        var result = TurnClassifier.Classify("Computer, lights");

        Assert.StartsWith(HolodeckPersona.ComputerMarker, result.MarkedText);
    }

    [Fact]
    public void MarkedText_WhenInSimulation_UsesSimulationMarker()
    {
        var result = TurnClassifier.Classify("I look around the room");

        Assert.StartsWith(HolodeckPersona.SimulationMarker, result.MarkedText);
    }
}
