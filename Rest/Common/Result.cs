namespace Rest.Common;

/// <summary>
///     Represents the result of an operation that may succeed or fail.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    ///     Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    ///     Gets the error if the operation failed.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    ///     Creates a failed result with an error.
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    ///     Creates a successful result with a value.
    /// </summary>
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    /// <summary>
    ///     Creates a failed result with an error.
    /// </summary>
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>
///     Represents the result of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    ///     Gets the value if the operation succeeded.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing value of a failed result.</exception>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Failure result has no value.");

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}

/// <summary>
///     Represents an error with a code and description.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Description">The error description.</param>
public record Error(string Code, string Description)
{
    /// <summary>
    ///     Represents no error (successful operation).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    ///     Represents a null value error.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "Value is null.");
}
