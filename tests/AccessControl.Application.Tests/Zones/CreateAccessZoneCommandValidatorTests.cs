using AccessControl.Application.Zones.Commands;
using FluentAssertions;

namespace AccessControl.Application.Tests.Zones;

public class CreateAccessZoneCommandValidatorTests
{
    private readonly CreateAccessZoneCommandValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateAccessZoneCommand("Office", "Main office zone");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldHaveError()
    {
        // Arrange
        var command = new CreateAccessZoneCommand("", null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAccessZoneCommand.Name));
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateAccessZoneCommand(new string('x', 101), null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAccessZoneCommand.Name));
    }

    [Fact]
    public void Validate_WithNameExactly100_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateAccessZoneCommand(new string('x', 100), null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateAccessZoneCommand("Office", new string('x', 501));

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAccessZoneCommand.Description));
    }

    [Fact]
    public void Validate_WithDescriptionExactly500_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateAccessZoneCommand("Office", new string('x', 500));

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullDescription_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateAccessZoneCommand("Office", null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
