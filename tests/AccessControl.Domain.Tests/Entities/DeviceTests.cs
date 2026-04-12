using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using AccessControl.Domain.Exceptions;
using FluentAssertions;

namespace AccessControl.Domain.Tests.Entities;

public class DeviceTests
{
    private readonly Guid _zoneId = Guid.NewGuid();
    private readonly Guid _hardwareId = Guid.NewGuid();

    private Device CreateValid(
        string name = "Front Door Reader",
        Guid? zoneId = null,
        Guid? hardwareId = null,
        DeviceAdapterType adapterType = DeviceAdapterType.CardReader,
        DeviceFeatures features = DeviceFeatures.CardReader)
    {
        return Device.Create(
            name,
            zoneId ?? _zoneId,
            hardwareId ?? _hardwareId,
            adapterType,
            features);
    }

    // --- Create ---

    [Fact]
    public void Create_WithValidData_ReturnsDeviceWithCorrectProperties()
    {
        // Act
        var device = CreateValid();

        // Assert
        device.Id.Should().NotBeEmpty();
        device.HardwareId.Should().Be(_hardwareId);
        device.Name.Should().Be("Front Door Reader");
        device.AdapterType.Should().Be(DeviceAdapterType.CardReader);
        device.Features.Should().Be(DeviceFeatures.CardReader);
        device.ZoneId.Should().Be(_zoneId);
        device.Status.Should().Be(DeviceStatus.Offline);
        device.LastHeartbeat.Should().BeNull();
        device.Configuration.Should().BeEmpty();
        device.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        device.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Act
        var act = () => CreateValid(name: name!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyZoneId_ThrowsDomainValidationException()
    {
        // Act
        var act = () => CreateValid(zoneId: Guid.Empty);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*ZoneId*");
    }

    [Fact]
    public void Create_WithEmptyHardwareId_ThrowsDomainValidationException()
    {
        // Act
        var act = () => CreateValid(hardwareId: Guid.Empty);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*HardwareId*");
    }

    [Fact]
    public void Create_TrimsNameWhitespace()
    {
        // Act
        var device = CreateValid(name: "  Front Door Reader  ");

        // Assert
        device.Name.Should().Be("Front Door Reader");
    }

    [Fact]
    public void Create_WithCombinedFeatures_SetsFeatures()
    {
        // Act
        var device = CreateValid(features: DeviceFeatures.CardReader | DeviceFeatures.LockControl);

        // Assert
        device.Features.Should().HaveFlag(DeviceFeatures.CardReader);
        device.Features.Should().HaveFlag(DeviceFeatures.LockControl);
    }

    // --- RecordHeartbeat ---

    [Fact]
    public void RecordHeartbeat_SetsStatusToOnline()
    {
        // Arrange
        var device = CreateValid();
        device.Status.Should().Be(DeviceStatus.Offline);

        // Act
        device.RecordHeartbeat();

        // Assert
        device.Status.Should().Be(DeviceStatus.Online);
    }

    [Fact]
    public void RecordHeartbeat_SetsLastHeartbeat()
    {
        // Arrange
        var device = CreateValid();
        device.LastHeartbeat.Should().BeNull();

        // Act
        device.RecordHeartbeat();

        // Assert
        device.LastHeartbeat.Should().NotBeNull();
        device.LastHeartbeat.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordHeartbeat_UpdatesTimestamp()
    {
        // Arrange
        var device = CreateValid();
        var originalTimestamp = device.UpdatedAt;

        // Act
        device.RecordHeartbeat();

        // Assert
        device.UpdatedAt.Should().BeOnOrAfter(originalTimestamp);
    }

    // --- Update ---

    [Fact]
    public void Update_WithValidData_UpdatesNameAndZoneId()
    {
        // Arrange
        var device = CreateValid();
        var newZoneId = Guid.NewGuid();

        // Act
        device.Update("Back Door Reader", newZoneId);

        // Assert
        device.Name.Should().Be("Back Door Reader");
        device.ZoneId.Should().Be(newZoneId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Update_WithInvalidName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var device = CreateValid();

        // Act
        var act = () => device.Update(name!, _zoneId);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithEmptyZoneId_ThrowsDomainValidationException()
    {
        // Arrange
        var device = CreateValid();

        // Act
        var act = () => device.Update("Valid Name", Guid.Empty);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*ZoneId*");
    }

    [Fact]
    public void Update_TrimsNameWhitespace()
    {
        // Arrange
        var device = CreateValid();

        // Act
        device.Update("  Back Door Reader  ", _zoneId);

        // Assert
        device.Name.Should().Be("Back Door Reader");
    }

    [Fact]
    public void Update_UpdatesTimestamp()
    {
        // Arrange
        var device = CreateValid();
        var originalTimestamp = device.UpdatedAt;

        // Act
        device.Update("Updated Name", _zoneId);

        // Assert
        device.UpdatedAt.Should().BeOnOrAfter(originalTimestamp);
    }

    // --- UpdateConfiguration ---

    [Fact]
    public void UpdateConfiguration_WithValidSettings_UpdatesConfiguration()
    {
        // Arrange
        var device = CreateValid();
        var settings = new Dictionary<string, string>
        {
            ["lockOpenDurationSec"] = "5",
            ["buzzerEnabled"] = "true",
        };

        // Act
        device.UpdateConfiguration(settings);

        // Assert
        device.Configuration.Should().HaveCount(2);
        device.Configuration["lockOpenDurationSec"].Should().Be("5");
        device.Configuration["buzzerEnabled"].Should().Be("true");
    }

    [Fact]
    public void UpdateConfiguration_WithInvalidKey_ThrowsDomainValidationException()
    {
        // Arrange
        var device = CreateValid();
        var settings = new Dictionary<string, string> { ["unknownKey"] = "value" };

        // Act
        var act = () => device.UpdateConfiguration(settings);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*unknownKey*");
    }

    [Fact]
    public void UpdateConfiguration_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var device = CreateValid();

        // Act
        var act = () => device.UpdateConfiguration(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateConfiguration_CanonicalizesKeyCase()
    {
        // Arrange
        var device = CreateValid();
        var settings = new Dictionary<string, string> { ["BUZZERENABLED"] = "true" };

        // Act
        device.UpdateConfiguration(settings);

        // Assert
        device.Configuration.Should().ContainKey("buzzerEnabled");
    }

    [Fact]
    public void UpdateConfiguration_OverwritesPreviousValues()
    {
        // Arrange
        var device = CreateValid();
        device.UpdateConfiguration(new Dictionary<string, string> { ["lockOpenDurationSec"] = "5" });

        // Act
        device.UpdateConfiguration(new Dictionary<string, string> { ["lockOpenDurationSec"] = "10" });

        // Assert
        device.Configuration["lockOpenDurationSec"].Should().Be("10");
    }

    [Fact]
    public void UpdateConfiguration_UpdatesTimestamp()
    {
        // Arrange
        var device = CreateValid();
        var originalTimestamp = device.UpdatedAt;

        // Act
        device.UpdateConfiguration(new Dictionary<string, string> { ["buzzerEnabled"] = "true" });

        // Assert
        device.UpdatedAt.Should().BeOnOrAfter(originalTimestamp);
    }

    [Fact]
    public void UpdateConfiguration_MultipleSettings_AllApplied()
    {
        // Arrange
        var device = CreateValid();
        var settings = new Dictionary<string, string>
        {
            ["lockOpenDurationSec"] = "10",
            ["heartbeatIntervalSec"] = "30",
            ["enrollmentTimeoutSec"] = "60",
            ["buzzerEnabled"] = "false",
            ["ledBrightness"] = "128",
        };

        // Act
        device.UpdateConfiguration(settings);

        // Assert
        device.Configuration.Should().HaveCount(5);
        device.Configuration["lockOpenDurationSec"].Should().Be("10");
        device.Configuration["heartbeatIntervalSec"].Should().Be("30");
        device.Configuration["enrollmentTimeoutSec"].Should().Be("60");
        device.Configuration["buzzerEnabled"].Should().Be("false");
        device.Configuration["ledBrightness"].Should().Be("128");
    }

    // --- UpdateConfiguration: lockOpenDurationSec ---

    [Theory]
    [InlineData("1")]
    [InlineData("30")]
    [InlineData("60")]
    public void UpdateConfiguration_LockOpenDurationSec_ValidRange_Succeeds(string value)
    {
        // Arrange
        var device = CreateValid();

        // Act
        device.UpdateConfiguration(new Dictionary<string, string> { ["lockOpenDurationSec"] = value });

        // Assert
        device.Configuration["lockOpenDurationSec"].Should().Be(value);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("61")]
    [InlineData("abc")]
    public void UpdateConfiguration_LockOpenDurationSec_InvalidRange_Throws(string value)
    {
        // Arrange
        var device = CreateValid();

        // Act
        var act = () => device.UpdateConfiguration(
            new Dictionary<string, string> { ["lockOpenDurationSec"] = value });

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    // --- UpdateConfiguration: heartbeatIntervalSec ---

    [Theory]
    [InlineData("5")]
    [InlineData("150")]
    [InlineData("300")]
    public void UpdateConfiguration_HeartbeatIntervalSec_ValidRange_Succeeds(string value)
    {
        // Arrange
        var device = CreateValid();

        // Act
        device.UpdateConfiguration(new Dictionary<string, string> { ["heartbeatIntervalSec"] = value });

        // Assert
        device.Configuration["heartbeatIntervalSec"].Should().Be(value);
    }

    [Theory]
    [InlineData("4")]
    [InlineData("301")]
    [InlineData("abc")]
    public void UpdateConfiguration_HeartbeatIntervalSec_InvalidRange_Throws(string value)
    {
        // Arrange
        var device = CreateValid();

        // Act
        var act = () => device.UpdateConfiguration(
            new Dictionary<string, string> { ["heartbeatIntervalSec"] = value });

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    // --- UpdateConfiguration: enrollmentTimeoutSec ---

    [Theory]
    [InlineData("1")]
    [InlineData("60")]
    [InlineData("120")]
    public void UpdateConfiguration_EnrollmentTimeoutSec_ValidRange_Succeeds(string value)
    {
        // Arrange
        var device = CreateValid();

        // Act
        device.UpdateConfiguration(new Dictionary<string, string> { ["enrollmentTimeoutSec"] = value });

        // Assert
        device.Configuration["enrollmentTimeoutSec"].Should().Be(value);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("121")]
    [InlineData("abc")]
    public void UpdateConfiguration_EnrollmentTimeoutSec_InvalidRange_Throws(string value)
    {
        // Arrange
        var device = CreateValid();

        // Act
        var act = () => device.UpdateConfiguration(
            new Dictionary<string, string> { ["enrollmentTimeoutSec"] = value });

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    // --- UpdateConfiguration: buzzerEnabled ---

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("True")]
    [InlineData("False")]
    public void UpdateConfiguration_BuzzerEnabled_ValidValues_Succeeds(string value)
    {
        // Arrange
        var device = CreateValid();

        // Act
        device.UpdateConfiguration(new Dictionary<string, string> { ["buzzerEnabled"] = value });

        // Assert
        device.Configuration["buzzerEnabled"].Should().Be(value);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("1")]
    [InlineData("abc")]
    public void UpdateConfiguration_BuzzerEnabled_InvalidValues_Throws(string value)
    {
        // Arrange
        var device = CreateValid();

        // Act
        var act = () => device.UpdateConfiguration(
            new Dictionary<string, string> { ["buzzerEnabled"] = value });

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    // --- UpdateConfiguration: ledBrightness ---

    [Theory]
    [InlineData("0")]
    [InlineData("128")]
    [InlineData("255")]
    public void UpdateConfiguration_LedBrightness_ValidRange_Succeeds(string value)
    {
        // Arrange
        var device = CreateValid();

        // Act
        device.UpdateConfiguration(new Dictionary<string, string> { ["ledBrightness"] = value });

        // Assert
        device.Configuration["ledBrightness"].Should().Be(value);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("256")]
    [InlineData("abc")]
    public void UpdateConfiguration_LedBrightness_InvalidRange_Throws(string value)
    {
        // Arrange
        var device = CreateValid();

        // Act
        var act = () => device.UpdateConfiguration(
            new Dictionary<string, string> { ["ledBrightness"] = value });

        // Assert
        act.Should().Throw<DomainValidationException>();
    }
}
