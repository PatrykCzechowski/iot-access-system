using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using FluentAssertions;

namespace AccessControl.Domain.Tests.Entities;

public class AccessProfileTests
{
    // --- Create ---

    [Fact]
    public void Create_WithValidName_ReturnsProfileWithCorrectProperties()
    {
        // Act
        var profile = AccessProfile.Create("Test Profile", "Test Description");

        // Assert
        profile.Id.Should().NotBeEmpty();
        profile.Name.Should().Be("Test Profile");
        profile.Description.Should().Be("Test Description");
        profile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        profile.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithNullDescription_SetsDescriptionToNull()
    {
        // Act
        var profile = AccessProfile.Create("Test Profile");

        // Assert
        profile.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => AccessProfile.Create(name!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_TrimsWhitespaceFromName()
    {
        // Act
        var profile = AccessProfile.Create("  Test Profile  ");

        // Assert
        profile.Name.Should().Be("Test Profile");
    }

    [Fact]
    public void Create_InitializesWithEmptyCollections()
    {
        // Act
        var profile = AccessProfile.Create("Test Profile");

        // Assert
        profile.AccessProfileZones.Should().BeEmpty();
        profile.Cardholders.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithNameExceeding100Chars_ThrowsDomainValidationException()
    {
        // Act
        var act = () => AccessProfile.Create(new string('a', 101));

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    // --- Update ---

    [Fact]
    public void Update_WithValidData_UpdatesNameAndDescription()
    {
        // Arrange
        var profile = AccessProfile.Create("Test Profile", "Test Description");

        // Act
        profile.Update("Updated Name", "Updated Description");

        // Assert
        profile.Name.Should().Be("Updated Name");
        profile.Description.Should().Be("Updated Description");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Update_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var profile = AccessProfile.Create("Test Profile");

        // Act
        var act = () => profile.Update(name!, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithNameExceeding100Chars_ThrowsDomainValidationException()
    {
        // Arrange
        var profile = AccessProfile.Create("Test Profile");

        // Act
        var act = () => profile.Update(new string('a', 101), null);

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Update_UpdatesTimestamp()
    {
        // Arrange
        var profile = AccessProfile.Create("Test Profile");
        var originalTimestamp = profile.UpdatedAt;

        // Act
        profile.Update("Updated Name", null);

        // Assert
        profile.UpdatedAt.Should().BeOnOrAfter(originalTimestamp);
    }

}
