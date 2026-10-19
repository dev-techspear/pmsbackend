namespace PMS.Application.Common;

public class ApiResponse<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];

    public static ApiResponse<T> Success(T data, string? message = null) => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) => new()
    {
        IsSuccess = false,
        Message = message,
        Errors = errors ?? []
    };
}

public class PaginatedResponse<T>
{
    public IEnumerable<T> Data { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}
