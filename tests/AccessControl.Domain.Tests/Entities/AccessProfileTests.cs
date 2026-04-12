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
        var accessProfile = AccessProfile.Create("Test Profile", "Test Description");

        // Assert
        accessProfile.Id.Should().NotBeEmpty();
        accessProfile.Name.Should().Be("Test Profile");
        accessProfile.Description.Should().Be("Test Description");
        accessProfile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        accessProfile.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithNullDescription_SetsDescriptionToNull()
    {
        // Act
        var accessProfile = AccessProfile.Create("Test Profile");

        // Assert
        accessProfile.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentException()
    {
        // Act
        var act = () => AccessProfile.Create(null!);

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
    public void Create_WithWhitespaceName_ThrowsArgumentException()
    {
        // Act
        var act = () => AccessProfile.Create("   ");

        // Assert
        act.Should().Throw<ArgumentException>();
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
        var updatedName = "Updated Name";
        var updatedDescription = "Updated Description";

        // Act
        var accessProfile = AccessProfile.Create("Test Profile", "Test Description");
        accessProfile.Update(updatedName, updatedDescription);

        // Assert
        accessProfile.Name.Should().Be(updatedName);
        accessProfile.Description.Should().Be(updatedDescription);
    }

    [Fact]
    public void Update_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var profile = AccessProfile.Create("Test Profile");

        // Act
        var act = () => profile.Update(null!, null);

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
