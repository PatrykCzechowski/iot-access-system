using AccessControl.Domain.Entities;
using FluentAssertions;

namespace AccessControl.Domain.Tests.Entities;

public class CardholderTests
{
    // --- Create ---

    [Fact]
    public void Create_WithValidData_ReturnsCardholderWithCorrectProperties()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Kowalski";
        var email = "john.kowalski@gmail.com";
        var phoneNumber = "123456789";

        // Act
        var cardholder = Cardholder.Create(firstName, lastName, email, phoneNumber);

        // Assert
        cardholder.Id.Should().NotBeEmpty();
        cardholder.FirstName.Should().Be(firstName);
        cardholder.LastName.Should().Be(lastName);
        cardholder.Email.Should().Be(email);
        cardholder.PhoneNumber.Should().Be(phoneNumber);
        cardholder.AccessProfiles.Should().BeEmpty();
        cardholder.Cards.Should().BeEmpty();
        cardholder.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        cardholder.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithNullOptionalFields_SetsEmailAndPhoneToNull()
    {
        // Act
        var cardholder = Cardholder.Create("John", "Kowalski");

        // Assert
        cardholder.Email.Should().BeNull();
        cardholder.PhoneNumber.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "Kowalski")]
    [InlineData("", "Kowalski")]
    [InlineData(" ", "Kowalski")]
    public void Create_WithInvalidFirstName_ThrowsArgumentException(string? firstName, string lastName)
    {
        // Act
        var act = () => Cardholder.Create(firstName!, lastName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("John", null)]
    [InlineData("John", "")]
    [InlineData("John", " ")]
    public void Create_WithInvalidLastName_ThrowsArgumentException(string firstName, string? lastName)
    {
        // Act
        var act = () => Cardholder.Create(firstName, lastName!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(" John ", "Kowalski", "john.kowalski@gmail.com", "123456789")]
    [InlineData("John", " Kowalski ", "john.kowalski@gmail.com", "123456789")]
    [InlineData("John", "Kowalski", " john.kowalski@gmail.com ", "123456789")]
    [InlineData("John", "Kowalski", "john.kowalski@gmail.com", " 123456789 ")]
    public void Create_TrimsWhitespace_InAllStringFields(string firstName, string lastName, string email, string phoneNumber)
    {
        // Act
        var cardholder = Cardholder.Create(firstName, lastName, email, phoneNumber);

        // Assert
        cardholder.FirstName.Should().Be(firstName.Trim());
        cardholder.LastName.Should().Be(lastName.Trim());
        cardholder.Email.Should().Be(email.Trim());
        cardholder.PhoneNumber.Should().Be(phoneNumber.Trim());
    }

    // --- FullName ---

    [Fact]
    public void FullName_ReturnsFirstNameAndLastName()
    {
        // Arrange
        var cardholder = Cardholder.Create("John", "Kowalski");

        // Act & Assert
        cardholder.FullName.Should().Be("John Kowalski");
    }

    // --- Update ---

    [Fact]
    public void Update_WithValidData_UpdatesAllFields()
    {
        // Arrange
        var cardholder = Cardholder.Create("Old", "Name", "old@x.pl", "000");

        // Act
        cardholder.Update("John", "Kowalski", "john.kowalski@gmail.com", "123456789");

        // Assert
        cardholder.FirstName.Should().Be("John");
        cardholder.LastName.Should().Be("Kowalski");
        cardholder.Email.Should().Be("john.kowalski@gmail.com");
        cardholder.PhoneNumber.Should().Be("123456789");
    }

    [Fact]
    public void Update_WithValidData_SetsUpdatedAt()
    {
        // Arrange
        var cardholder = Cardholder.Create("Old", "Name");
        var createdAt = cardholder.UpdatedAt;

        // Act
        cardholder.Update("John", "Kowalski", null, null);

        // Assert
        cardholder.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        cardholder.UpdatedAt.Should().BeOnOrAfter(createdAt);
    }

    [Fact]
    public void Update_WithNullOptionalFields_ClearsEmailAndPhone()
    {
        // Arrange
        var cardholder = Cardholder.Create("John", "Kowalski", "john@x.pl", "123");

        // Act
        cardholder.Update("John", "Kowalski", null, null);

        // Assert
        cardholder.Email.Should().BeNull();
        cardholder.PhoneNumber.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "Kowalski")]
    [InlineData("", "Kowalski")]
    [InlineData(" ", "Kowalski")]
    public void Update_WithInvalidFirstName_ThrowsArgumentException(string? firstName, string lastName)
    {
        // Arrange
        var cardholder = Cardholder.Create("John", "Kowalski");

        // Act
        var act = () => cardholder.Update(firstName!, lastName, null, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("John", null)]
    [InlineData("John", "")]
    [InlineData("John", " ")]
    public void Update_WithInvalidLastName_ThrowsArgumentException(string firstName, string? lastName)
    {
        // Arrange
        var cardholder = Cardholder.Create("John", "Kowalski");

        // Act
        var act = () => cardholder.Update(firstName, lastName!, null, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
