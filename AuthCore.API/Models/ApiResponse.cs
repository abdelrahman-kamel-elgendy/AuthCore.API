using System.Text.Json.Serialization;

namespace AuthCore.API.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? ValidationErrors { get; set; }

    public ApiResponse()
    {
        Success = true;
    }

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> Ok(string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message
        };
    }

    public static ApiResponse<T> Fail(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    public static ApiResponse<T> Fail(string message, IDictionary<string, string[]> validationErrors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ValidationErrors = validationErrors
        };
    }
}