using System.Text.Json.Serialization;

namespace AuthCore.API.Models;

public class ApiResponse<T>(bool? success, T? data, string? message)
{
    public bool Success { get; set; } = success ?? true;
    public string? Message { get; set; } = message;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; } = data;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, List<string>>? ValidationErrors { get; set; }
}