using Xunit;
using FluentAssertions;
using AcoTestApi.Application.Printer.Commands;

namespace AcoTestApi.Tests;

public class ValidatorTests
{
    private readonly ConnectCommandValidator _connectValidator;
    private readonly PrintTextCommandValidator _printTextValidator;

    public ValidatorTests()
    {
        _connectValidator = new ConnectCommandValidator();
        _printTextValidator = new PrintTextCommandValidator();
    }

    [Theory]
    [InlineData("usb", true)]
    [InlineData("lan", true)]
    [InlineData("none", true)]
    [InlineData("USB", true)]
    [InlineData("LAN", true)]
    [InlineData("NONE", true)]
    [InlineData("bluetooth", false)]
    [InlineData("", false)]
    public void ConnectCommandValidator_ShouldValidateCorrectModes(string mode, bool expectedValid)
    {
        // Arrange
        var command = new ConnectCommand(mode);

        // Act
        var result = _connectValidator.Validate(command);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void PrintTextCommandValidator_ShouldFail_WhenTextIsEmpty()
    {
        // Arrange
        var command = new PrintTextCommand("", "tr");

        // Act
        var result = _printTextValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Text");
    }

    [Theory]
    [InlineData("tr", true)]
    [InlineData("en", true)]
    [InlineData("fr", false)]
    [InlineData("de", false)]
    public void PrintTextCommandValidator_ShouldValidateCorrectLanguages(string lang, bool expectedValid)
    {
        // Arrange
        var command = new PrintTextCommand("Hello World", lang);

        // Act
        var result = _printTextValidator.Validate(command);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }
}
