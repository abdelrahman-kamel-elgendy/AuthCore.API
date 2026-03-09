using System.ComponentModel.DataAnnotations;

namespace AuthCore.API.DTOs.Emai;

public class ConfirmEmailDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}