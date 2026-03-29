namespace AccessControl.Application.Common.Interfaces;

public interface ICardEnrollmentService
{
    Task<CardEnrollmentResult> EnrollCardAsync(Guid deviceHardwareId, string cardUid, CancellationToken cancellationToken);
}

public record CardEnrollmentResult(bool Success, string CardUid, string Message, Guid? CardId = null);
