using AccessControl.Application.Cardholders.Commands;
using FluentAssertions;

namespace AccessControl.Application.Tests.Cardholders;

public class CreateCardholderCommandValidatorTests
{
    private readonly CreateCardholderCommandValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateCardholderCommand("Jan", "Kowalski", "jan@x.pl", "+48123456789");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyFirstName_ShouldHaveError()
    {
        // Arrange
        var command = new CreateCardholderCommand("", "Kowalski", null, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCardholderCommand.FirstName));
    }

    [Fact]
    public void Validate_WithEmptyLastName_ShouldHaveError()
    {
        // Arrange
        var command = new CreateCardholderCommand("Jan", "", null, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCardholderCommand.LastName));
    }

    [Fact]
    public void Validate_WithFirstNameTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateCardholderCommand(new string('x', 101), "Kowalski", null, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCardholderCommand.FirstName));
    }

    [Fact]
    public void Validate_WithLastNameTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateCardholderCommand("Jan", new string('x', 101), null, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCardholderCommand.LastName));
    }

    [Fact]
    public void Validate_WithEmailTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateCardholderCommand("Jan", "Kowalski", new string('x', 101), null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCardholderCommand.Email));
    }

    [Fact]
    public void Validate_WithPhoneNumberTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateCardholderCommand("Jan", "Kowalski", null, new string('1', 21));

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCardholderCommand.PhoneNumber));
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_ShouldHaveError()
    {
        // Arrange
        var command = new CreateCardholderCommand("Jan", "Kowalski", "not-email", null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCardholderCommand.Email));
    }

    [Fact]
    public void Validate_WithValidEmail_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateCardholderCommand("Jan", "Kowalski", "jan@x.pl", null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAccessProfileIdsContainingEmptyGuid_ShouldHaveError()
    {
        // Arrange
        var command = new CreateCardholderCommand("Jan", "Kowalski", null, null,
            new List<Guid> { Guid.NewGuid(), Guid.Empty });

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains(nameof(CreateCardholderCommand.AccessProfileIds)));
    }

    [Fact]
    public void Validate_WithDuplicateAccessProfileIds_ShouldHaveError()
    {
        // Arrange
        var duplicateId = Guid.NewGuid();
        var command = new CreateCardholderCommand("Jan", "Kowalski", null, null,
            new List<Guid> { duplicateId, duplicateId });

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Duplicate"));
    }

    [Fact]
    public void Validate_WithNullAccessProfileIds_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateCardholderCommand("Jan", "Kowalski", null, null, null);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
