namespace AccessControl.Application.Devices.DTOs;

public record DeviceProvisionResult(bool Success, string? Error = null);
