namespace AuthCore.API.Services.Interfaces;

public interface IEmailService
{
    Task SendConfirmationEmailAsync(string email, string firstName, string confirmationUrl);
    Task SendPasswordResetEmailAsync(string? email, string firstName, string resetUrl);
    Task SendWelcomeEmailAsync(string? email, string firstName, string v1, string v2, string loginUrl);
}
