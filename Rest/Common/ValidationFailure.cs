namespace Rest.Common;

public record ValidationFailure(string PropertyName, string ErrorMessage);