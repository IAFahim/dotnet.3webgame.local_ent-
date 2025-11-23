using Rest.Common;

namespace Rest.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public class ValidationException(IEnumerable<ValidationFailure> errors)
    : DomainException("One or more validation errors occurred.")
{
    public IEnumerable<ValidationFailure> Errors { get; } = errors;
}

public class NotFoundException(string resource, string key)
    : DomainException($"{resource} with key '{key}' was not found.");

public class UnauthorizedException(string message)
    : DomainException(message);
