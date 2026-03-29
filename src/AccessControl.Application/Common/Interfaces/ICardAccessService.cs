namespace AccessControl.Application.Common.Interfaces;

public interface ICardAccessService
{
    Task<CardAccessResult> ValidateAndGrantAccessAsync(Guid deviceHardwareId, string cardUid, CancellationToken cancellationToken);
}

public record CardAccessResult(bool Granted, string Uid, string Message);
