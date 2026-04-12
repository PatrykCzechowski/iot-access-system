using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using FluentAssertions;

namespace AccessControl.Domain.Tests.Entities;

public class AccessZoneTests
{
    // --- Create ---

    [Fact]
    public void Create_WithValidName_ReturnsZoneWithCorrectProperties()
    {
        // Act
        var zone = AccessZone.Create("Main Entrance", "Front door area");

        // Assert
        zone.Id.Should().NotBeEmpty();
        zone.Name.Should().Be("Main Entrance");
        zone.Description.Should().Be("Front door area");
        zone.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        zone.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithNullDescription_SetsDescriptionToNull()
    {
        // Act
        var zone = AccessZone.Create("Main Entrance");

        // Assert
        zone.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithCustomId_UsesProvidedId()
    {
        // Arrange
        var customId = Guid.NewGuid();

        // Act
        var zone = AccessZone.Create("Main Entrance", id: customId);

        // Assert
        zone.Id.Should().Be(customId);
    }

    [Fact]
    public void Create_WithoutCustomId_GeneratesNewId()
    {
        // Act
        var zone = AccessZone.Create("Main Entrance");

        // Assert
        zone.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => AccessZone.Create(name!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNameExceeding100Chars_ThrowsDomainValidationException()
    {
        // Act
        var act = () => AccessZone.Create(new string('a', 101));

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Create_WithNameExactly100Chars_Succeeds()
    {
        // Act
        var zone = AccessZone.Create(new string('a', 100));

        // Assert
        zone.Name.Should().HaveLength(100);
    }

    [Fact]
    public void Create_WithDescriptionExceeding500Chars_ThrowsDomainValidationException()
    {
        // Act
        var act = () => AccessZone.Create("Valid Name", new string('a', 501));

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Create_WithDescriptionExactly500Chars_Succeeds()
    {
        // Act
        var zone = AccessZone.Create("Valid Name", new string('a', 500));

        // Assert
        zone.Description.Should().HaveLength(500);
    }

    [Fact]
    public void Create_TrimsNameWhitespace()
    {
        // Act
        var zone = AccessZone.Create("  Main Entrance  ");

        // Assert
        zone.Name.Should().Be("Main Entrance");
    }

    [Fact]
    public void Create_TrimsDescriptionWhitespace()
    {
        // Act
        var zone = AccessZone.Create("Main Entrance", "  Front door area  ");

        // Assert
        zone.Description.Should().Be("Front door area");
    }

    [Fact]
    public void Create_InitializesEmptyAccessProfileZonesCollection()
    {
        // Act
        var zone = AccessZone.Create("Main Entrance");

        // Assert
        zone.AccessProfileZones.Should().BeEmpty();
    }

    // --- UpdateName ---

    [Fact]
    public void UpdateName_WithValidName_UpdatesName()
    {
        // Arrange
        var zone = AccessZone.Create("Old Name");

        // Act
        zone.UpdateName("New Name");

        // Assert
        zone.Name.Should().Be("New Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UpdateName_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var zone = AccessZone.Create("Valid Name");

        // Act
        var act = () => zone.UpdateName(name!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateName_WithNameExceeding100Chars_ThrowsDomainValidationException()
    {
        // Arrange
        var zone = AccessZone.Create("Valid Name");

        // Act
        var act = () => zone.UpdateName(new string('a', 101));

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void UpdateName_TrimsWhitespace()
    {
        // Arrange
        var zone = AccessZone.Create("Old Name");

        // Act
        zone.UpdateName("  New Name  ");

        // Assert
        zone.Name.Should().Be("New Name");
    }

    [Fact]
    public void UpdateName_UpdatesTimestamp()
    {
        // Arrange
        var zone = AccessZone.Create("Old Name");
        var originalTimestamp = zone.UpdatedAt;

        // Act
        zone.UpdateName("New Name");

        // Assert
        zone.UpdatedAt.Should().BeOnOrAfter(originalTimestamp);
    }

    // --- UpdateDescription ---

    [Fact]
    public void UpdateDescription_WithValidDescription_UpdatesDescription()
    {
        // Arrange
        var zone = AccessZone.Create("Zone", "Old description");

        // Act
        zone.UpdateDescription("New description");

        // Assert
        zone.Description.Should().Be("New description");
    }

    [Fact]
    public void UpdateDescription_WithNull_ClearsDescription()
    {
        // Arrange
        var zone = AccessZone.Create("Zone", "Some description");

        // Act
        zone.UpdateDescription(null);

        // Assert
        zone.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateDescription_WithDescriptionExceeding500Chars_ThrowsDomainValidationException()
    {
        // Arrange
        var zone = AccessZone.Create("Zone");

        // Act
        var act = () => zone.UpdateDescription(new string('a', 501));

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void UpdateDescription_TrimsWhitespace()
    {
        // Arrange
        var zone = AccessZone.Create("Zone");

        // Act
        zone.UpdateDescription("  New description  ");

        // Assert
        zone.Description.Should().Be("New description");
    }

    [Fact]
    public void UpdateDescription_UpdatesTimestamp()
    {
        // Arrange
        var zone = AccessZone.Create("Zone");
        var originalTimestamp = zone.UpdatedAt;

        // Act
        zone.UpdateDescription("New description");

        // Assert
        zone.UpdatedAt.Should().BeOnOrAfter(originalTimestamp);
    }
}
