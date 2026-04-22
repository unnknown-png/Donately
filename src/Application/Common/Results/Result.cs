namespace Donately.Application.Common.Results;

public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public bool IsNone => string.IsNullOrWhiteSpace(Code);

    public override string ToString() => IsNone ? string.Empty : $"{Code}: {Message}";
}

public readonly record struct Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    private Result(bool isSuccess, Error error)
    {
        if (isSuccess && !error.IsNone)
        {
            throw new InvalidOperationException("Successful result cannot contain an error.");
        }

        if (!isSuccess && error.IsNone)
        {
            throw new InvalidOperationException("Failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);

    public override string ToString() => IsSuccess ? "Success" : $"Failure: {Error}";
}

public readonly record struct Result<T>
{
    private readonly T? _value;

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    private Result(T? value, bool isSuccess, Error error)
    {
        if (isSuccess && !error.IsNone)
        {
            throw new InvalidOperationException("Successful result cannot contain an error.");
        }

        if (!isSuccess && error.IsNone)
        {
            throw new InvalidOperationException("Failed result must contain an error.");
        }

        _value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value, true, Error.None);

    public static Result<T> Failure(Error error) => new(default, false, error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";
}


