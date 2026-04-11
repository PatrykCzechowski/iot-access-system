using AccessControl.Domain.Entities;
using FluentAssertions;

namespace AccessControl.Domain.Tests.Entities;

public class AccessCardTests
{
    [Fact]
    public void Create_WithValidUid_ReturnsCardWithCorrectProperties()
    {
        // Arrange
        var uid = "ab:cd:ef:12";
        var label = "Karta wejściowa";

        // Act
        var card = AccessCard.Create(uid, label);

        // Assert
        card.Id.Should().NotBeEmpty();
        card.CardUid.Should().Be("AB:CD:EF:12");
        card.Label.Should().Be("Karta wejściowa");
        card.IsActive.Should().BeTrue();
        card.CardholderId.Should().BeNull();
        card.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithEmptyUid_ThrowsArgumentException(string? uid)
    {
        // Act
        var act = () => AccessCard.Create(uid!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithLabel_TrimsWhiteSpace()
    {
        // Act
        var card = AccessCard.Create("AABB", "  main door  ");

        // Assert
        card.Label.Should().Be("main door");
    }

    [Fact]
    public void Create_WithNullLabel_SetsLabelToNull()
    {
        // Act
        var card = AccessCard.Create("AABB");

        // Assert
        card.Label.Should().BeNull();
    }

    // NormalizeUid


}
