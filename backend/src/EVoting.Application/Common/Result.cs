namespace EVoting.Application.Common;

public enum AppError
{
    None,
    DuplicateEmail,
    InvalidCredentials,
    ValidationFailed,
    NotFound,
    ElectionNotActive,
    InvalidCandidate,
    AlreadyVoted
}

public class Result<T>
{
    public bool Succeeded { get; }
    public T? Value { get; }
    public AppError Error { get; }
    public string? ErrorMessage { get; }

    private Result(bool succeeded, T? value, AppError error, string? errorMessage)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T value) => new(true, value, AppError.None, null);

    public static Result<T> Failure(AppError error, string errorMessage) =>
        new(false, default, error, errorMessage);
}
