namespace ExpenseManagement.Models;

public class ApiError
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? File { get; set; }
    public int? Line { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    
    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(string message, string? details = null, string? file = null, int? line = null) 
        => new() { Success = false, Error = new ApiError { Message = message, Details = details, File = file, Line = line } };
}
