using System.Net;
using System.Text.Json.Serialization;

namespace AuthCore.API.Models;

public class ApiResponse<T>
{
    public HttpStatusCode Status { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? ValidationErrors { get; set; }

    
    
    public ApiResponse(bool success) => Success = success;

    public ApiResponse(HttpStatusCode status, bool success, string? message, T? data)
    {
        Status = status;
        Success = success;
        Message = message;
        Data = data;
    }
}