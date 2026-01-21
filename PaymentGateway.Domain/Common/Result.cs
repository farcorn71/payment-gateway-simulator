

namespace PaymentGateway.Domain.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public Error? Error { get; }

        private Result(T value)
        {
            IsSuccess = true;
            Value = value;
            Error = null;
        }

        private Result(Error error)
        {
            IsSuccess = false;
            Value = default;
            Error = error;
        }

        public static Result<T> Success(T value) => new(value);
        public static Result<T> Failure(Error error) => new(error);

        public TResult Match<TResult>(
            Func<T, TResult> onSuccess,
            Func<Error, TResult> onFailure) =>
            IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }

    public record Error(string Code, string Message, ErrorType Type = ErrorType.Validation)
    {
        public static Error Validation(string code, string message) =>
            new(code, message, ErrorType.Validation);

        public static Error NotFound(string code, string message) =>
            new(code, message, ErrorType.NotFound);

        public static Error Conflict(string code, string message) =>
            new(code, message, ErrorType.Conflict);

        public static Error External(string code, string message) =>
            new(code, message, ErrorType.External);
    }

    public enum ErrorType
    {
        Validation,
        NotFound,
        Conflict,
        External
    }
}
