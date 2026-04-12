using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using FluentAssertions;

namespace AccessControl.Domain.Tests.Entities;

public class AccessLogTests
{
    private readonly Guid _deviceId = Guid.NewGuid();
    private readonly Guid _zoneId = Guid.NewGuid();

    private AccessLog CreateValid(
        string cardUid = "AB:CD:EF:01",
        string? deviceName = "Front Door Reader",
        string? zoneName = "Main Entrance",
        string? userName = "Jan Kowalski",
        bool accessGranted = true,
        string? message = null)
    {
        return AccessLog.Create(
            cardUid,
            _deviceId,
            deviceName!,
            _zoneId,
            zoneName!,
            userName,
            accessGranted,
            message);
    }

    // --- Create (happy path) ---

    [Fact]
    public void Create_WithValidData_ReturnsLogWithCorrectProperties()
    {
        // Act
        var log = CreateValid();

        // Assert
        log.Id.Should().NotBeEmpty();
        log.CardUid.Should().Be("AB:CD:EF:01");
        log.DeviceId.Should().Be(_deviceId);
        log.DeviceName.Should().Be("Front Door Reader");
        log.ZoneId.Should().Be(_zoneId);
        log.ZoneName.Should().Be("Main Entrance");
        log.UserName.Should().Be("Jan Kowalski");
        log.AccessGranted.Should().BeTrue();
        log.Message.Should().BeNull();
        log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithAccessDenied_SetsAccessGrantedToFalse()
    {
        // Act
        var log = CreateValid(accessGranted: false, message: "Card not registered");

        // Assert
        log.AccessGranted.Should().BeFalse();
        log.Message.Should().Be("Card not registered");
    }

    [Fact]
    public void Create_WithNullUserName_SetsUserNameToNull()
    {
        // Act
        var log = CreateValid(userName: null);

        // Assert
        log.UserName.Should().BeNull();
    }

    // --- CardUid normalization ---

    [Fact]
    public void Create_NormalizesCardUidToUpperCase()
    {
        // Act
        var log = CreateValid(cardUid: "ab:cd:ef:01");

        // Assert
        log.CardUid.Should().Be("AB:CD:EF:01");
    }

    [Fact]
    public void Create_TrimsCardUidWhitespace()
    {
        // Act
        var log = CreateValid(cardUid: "  AB:CD:EF:01  ");

        // Assert
        log.CardUid.Should().Be("AB:CD:EF:01");
    }

    // --- DeviceName trimming ---

    [Fact]
    public void Create_TrimsDeviceNameWhitespace()
    {
        // Act
        var log = CreateValid(deviceName: "  Front Door Reader  ");

        // Assert
        log.DeviceName.Should().Be("Front Door Reader");
    }

    // --- Validation: string fields ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidCardUid_ThrowsArgumentException(string? cardUid)
    {
        // Act
        var act = () => CreateValid(cardUid: cardUid!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidDeviceName_ThrowsArgumentException(string? deviceName)
    {
        // Act
        var act = () => CreateValid(deviceName: deviceName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidZoneName_ThrowsArgumentException(string? zoneName)
    {
        // Act
        var act = () => CreateValid(zoneName: zoneName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    // --- Validation: DeviceId / ZoneId ---

    [Fact]
    public void Create_WithEmptyDeviceId_ThrowsDomainValidationException()
    {
        // Act
        var act = () => AccessLog.Create(
            "AB:CD:EF:01", Guid.Empty, "Reader", _zoneId, "Zone", null, true);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*DeviceId*");
    }

    [Fact]
    public void Create_WithEmptyZoneId_ThrowsDomainValidationException()
    {
        // Act
        var act = () => AccessLog.Create(
            "AB:CD:EF:01", _deviceId, "Reader", Guid.Empty, "Zone", null, true);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*ZoneId*");
    }

    // --- Message ---

    [Fact]
    public void Create_WithMessage_SetsMessage()
    {
        // Act
        var log = CreateValid(message: "Door held open");

        // Assert
        log.Message.Should().Be("Door held open");
    }

    [Fact]
    public void Create_WithGrantedAndMessage_BothAreSet()
    {
        // Act
        var log = CreateValid(accessGranted: true, message: "VIP access");

        // Assert
        log.AccessGranted.Should().BeTrue();
        log.Message.Should().Be("VIP access");
    }
}
