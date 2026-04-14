using AccessControl.Application.Cards.Commands;
using FluentAssertions;

namespace AccessControl.Application.Tests.Cards;

public class CreateAccessCardCommandValidatorTests
{
    private readonly CreateAccessCardCommandValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateAccessCardCommand("AB:CD:EF:12", Guid.NewGuid(), "Main door");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCardUid_ShouldHaveError()
    {
        // Arrange
        var command = new CreateAccessCardCommand("", null, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAccessCardCommand.CardUid));
    }

    [Fact]
    public void Validate_WithCardUidTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateAccessCardCommand(new string('A', 21), null, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAccessCardCommand.CardUid));
    }

    [Fact]
    public void Validate_WithCardUidExactly20_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateAccessCardCommand(new string('A', 20), null, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyGuidCardholderId_ShouldHaveError()
    {
        // Arrange
        var command = new CreateAccessCardCommand("AB:CD", Guid.Empty, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAccessCardCommand.CardholderId));
    }

    [Fact]
    public void Validate_WithNullCardholderId_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateAccessCardCommand("AB:CD", null, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithLabelTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateAccessCardCommand("AB:CD", null, new string('x', 201));

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAccessCardCommand.Label));
    }

    [Fact]
    public void Validate_WithLabelExactly200_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateAccessCardCommand("AB:CD", null, new string('x', 200));

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullLabel_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateAccessCardCommand("AB:CD", null, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
