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
        var label = "Access card";

        // Act
        var card = AccessCard.Create(uid, label);

        // Assert
        card.Id.Should().NotBeEmpty();
        card.CardUid.Should().Be("AB:CD:EF:12");
        card.Label.Should().Be("Access card");
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

    [Theory]
    [InlineData("ab:cd:ef:12", "AB:CD:EF:12")]
    [InlineData("  ab:cd:ef:12  ", "AB:CD:EF:12")]
    [InlineData("Ab:Cd:Ef:12", "AB:CD:EF:12")]
    public void NormalizeUid_VariousInputs_ReturnsUppercaseTrimmed(string input, string expected)
    {
        // Act
        var result = AccessCard.NormalizeUid(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void AssignCardholder_WithValidId_SetsCardholderId()
    {
        // Arrange
        var card = AccessCard.Create("AABB");
        var cardholder = Guid.NewGuid();

        // Act
        card.AssignCardholder(cardholder);

        // Assert
        card.CardholderId.Should().Be(cardholder);
    }

    [Fact]
    public void UnassignCardholder_WhenAssigned_ClearsCardholderId()
    {
        // Arrange
        var card = AccessCard.Create("AABB");
        card.AssignCardholder(Guid.NewGuid());

        // Act
        card.UnassignCardholder();

        // Assert
        card.CardholderId.Should().BeNull();
    }

    [Fact]
    public void AssignCardholder_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        var card = AccessCard.Create("AABB");

        // Act
        var act = () => card.AssignCardholder(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Update_WithNewValues_UpdatesLabelAndIsActive()
    {
        // Arrange
        var card = AccessCard.Create("AABB", "Old label");

        // Act
        card.Update("New label", false);

        // Assert
        card.Label.Should().Be("New label");
        card.IsActive.Should().BeFalse();
        card.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deactivate_WhenActive_SetsIsActiveToFalse()
    {
        // Arrange
        var card = AccessCard.Create("AABB");
        card.IsActive.Should().BeTrue();

        // Act
        card.Deactivate();

        // Assert
        card.IsActive.Should().BeFalse();
    }

}
