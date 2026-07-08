namespace EVoting.Application.Common;

public enum AuthError
{
    None,
    DuplicateEmail,
    InvalidCredentials,
    ValidationFailed
}

public class Result<T>
{
    public bool Succeeded { get; }
    public T? Value { get; }
    public AuthError Error { get; }
    public string? ErrorMessage { get; }

    private Result(bool succeeded, T? value, AuthError error, string? errorMessage)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T value) => new(true, value, AuthError.None, null);

    public static Result<T> Failure(AuthError error, string errorMessage) =>
        new(false, default, error, errorMessage);
}
