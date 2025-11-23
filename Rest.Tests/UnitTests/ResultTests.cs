using FluentAssertions;
using Rest.Common;

namespace Rest.Tests.UnitTests;

[TestFixture]
public class ResultTests
{
    [Test]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Test]
    public void Failure_ShouldCreateFailedResult()
    {
        // Arrange
        var error = new Error("TestCode", "Test error message");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Test]
    public void Success_WithValue_ShouldCreateSuccessfulResultWithValue()
    {
        // Arrange
        var testValue = "test value";

        // Act
        var result = Result.Success(testValue);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(testValue);
        result.Error.Should().Be(Error.None);
    }

    [Test]
    public void Failure_WithValue_ShouldCreateFailedResultWithoutValue()
    {
        // Arrange
        var error = new Error("TestCode", "Test error message");

        // Act
        var result = Result.Failure<string>(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Test]
    public void Value_OnFailedResult_ShouldThrowException()
    {
        // Arrange
        var error = new Error("TestCode", "Test error message");
        var result = Result.Failure<string>(error);

        // Act & Assert
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Failure result has no value.");
    }

    [Test]
    public void ImplicitConversion_FromValue_ShouldCreateSuccessResult()
    {
        // Act
        Result<int> result = 42;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Test]
    public void ImplicitConversion_FromError_ShouldCreateFailureResult()
    {
        // Arrange
        var error = new Error("TestCode", "Test error");

        // Act
        Result<int> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
