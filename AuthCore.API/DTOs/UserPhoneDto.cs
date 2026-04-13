namespace AuthCore.API.DTOs;

public class UserPhoneDto
{
    public string CountryCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool PhoneConfirmed { get; set; }
    public bool IsPrimary { get; set; }
}