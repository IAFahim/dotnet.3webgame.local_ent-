using Rest.Common; // Helper for ValidationFailure

namespace Rest.Exceptions;

// 1. The Base Exception
public abstract class DomainException(string message) : Exception(message);

// 2. The Validation Exception (Structured)
public class ValidationException(IEnumerable<ValidationFailure> errors) 
    : DomainException("One or more validation errors occurred.")
{
    public IEnumerable<ValidationFailure> Errors { get; } = errors;
}

// 3. Other Standard Exceptions
public class NotFoundException(string resource, string key) 
    : DomainException($"{resource} with key '{key}' was not found.");

public class UnauthorizedException(string message) 
    : DomainException(message);