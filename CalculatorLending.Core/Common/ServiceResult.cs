namespace CalculatorLending.Core.Common;

public class ServiceResult<T>
{
    public ResultStatus Status { get; }
    public T? Data { get; }
    public string? Error { get; }

    private ServiceResult(ResultStatus status, T? data, string? error)
    {
        Status = status;
        Data = data;
        Error = error;
    }

    public static ServiceResult<T> Success(T data) => new(ResultStatus.Success, data, null);
    public static ServiceResult<T> NotFound(string message) => new(ResultStatus.NotFound, default, message);
    public static ServiceResult<T> Validation(string message) => new(ResultStatus.Validation, default, message);
    public static ServiceResult<T> Conflict(string message) => new(ResultStatus.Conflict, default, message);
    public static ServiceResult<T> Forbidden(string message) => new(ResultStatus.Forbidden, default, message);
    public static ServiceResult<T> Unauthorized(string message) => new(ResultStatus.Unauthorized, default, message);
}

public class ServiceResult
{
    public ResultStatus Status { get; }
    public string? Error { get; }

    private ServiceResult(ResultStatus status, string? error)
    {
        Status = status;
        Error = error;
    }

    public static ServiceResult Success() => new(ResultStatus.Success, null);
    public static ServiceResult NotFound(string message) => new(ResultStatus.NotFound, message);
    public static ServiceResult Validation(string message) => new(ResultStatus.Validation, message);
    public static ServiceResult Conflict(string message) => new(ResultStatus.Conflict, message);
    public static ServiceResult Forbidden(string message) => new(ResultStatus.Forbidden, message);
    public static ServiceResult Unauthorized(string message) => new(ResultStatus.Unauthorized, message);
}
