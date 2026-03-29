namespace AccessControl.Domain.Exceptions;

public class DomainValidationException(string message) : Exception(message);
