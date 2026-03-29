# Pliki poprawione w ramach CR Fix (2026-03-29)

Poniższe pliki zostały zmodyfikowane w ramach wdrażania poprawek z Code Review.
Przy kolejnej iteracji CR można je wykluczyć z zakresu przeglądu.

## Iteracja 1

- `src/AccessControl.Application/Cards/Commands/CreateAccessCardCommandHandler.cs`
- `src/AccessControl.Domain/Entities/Device.cs`
- `src/AccessControl.Domain/Entities/AccessCard.cs`
- `src/AccessControl.Api/Infrastructure/GlobalExceptionHandler.cs`
- `src/AccessControl.Infrastructure/Mqtt/MqttClientService.cs`
- `src/AccessControl.Infrastructure/Mqtt/MqttBrokerMdnsAdvertiser.cs`

## Iteracja 2

- `src/AccessControl.Infrastructure/Mqtt/CardEnrollmentService.cs`
- `src/AccessControl.Application/Devices/Commands/CancelEnrollmentCommandHandler.cs`
- `src/AccessControl.Infrastructure/Mqtt/Handlers/CardReadMqttHandler.cs`
- `src/AccessControl.Infrastructure/Mqtt/Handlers/CardEnrolledMqttHandler.cs`
- `src/AccessControl.Infrastructure/Devices/Discovery/DeviceDiscoveryService.cs`
- `src/AccessControl.Api/Endpoints/CardEndpoints.cs`
- `src/AccessControl.Api/Endpoints/DeviceEndpoints.cs`
- `src/AccessControl.Infrastructure/Mqtt/Handlers/AnnounceMqttHandler.cs`

## Iteracja 3

- `src/AccessControl.Infrastructure/Mqtt/CardAccessService.cs`
- `src/AccessControl.Application/Devices/Commands/UpdateDeviceConfigCommandHandler.cs`
- `src/AccessControl.Infrastructure/Mqtt/Handlers/ConfigAckMqttHandler.cs`
- `src/AccessControl.Infrastructure/Mqtt/Handlers/LockStatusMqttHandler.cs`
- `src/AccessControl.Application/Common/Interfaces/IDeviceRepository.cs`
- `src/AccessControl.Infrastructure/Devices/DeviceRepository.cs`
