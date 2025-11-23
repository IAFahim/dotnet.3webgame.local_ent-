using FluentValidation;
using MediatR;
using Rest.Common;
using ValidationException = Rest.Exceptions.ValidationException;

// Assuming you have a ValidationException definition

namespace Rest.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = failures
            .Where(result => !result.IsValid)
            .SelectMany(result => result.Errors)
            .Select(f => new ValidationFailure(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (errors.Any())
        {
            throw new ValidationException(errors);
        }

        return await next();
    }
}
