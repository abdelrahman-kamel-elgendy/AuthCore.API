using System.Net;
using System.Net.Mail;
using AuthCore.API.Services.Interfaces;

namespace AuthCore.API.Services;

public class EmailService(IConfiguration config) : IEmailService
{
    private readonly IConfiguration _config = config;

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtp = _config.GetSection("Smtp");
        var host = smtp["Host"] ?? throw new InvalidOperationException("Smtp:Host not configured.");
        var port = int.Parse(smtp["Port"] ?? "587");
        var user = smtp["Username"] ?? throw new InvalidOperationException("Smtp:Username not configured.");
        var pass = smtp["Password"] ?? throw new InvalidOperationException("Smtp:Password not configured.");
        var fromName = smtp["FromName"] ?? "AuthCore";

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(user, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }

    private static string Render(string templateName, Dictionary<string, string> values)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", templateName);
        var html = File.ReadAllText(templatePath);

        foreach (var (key, value) in values)
            html = html.Replace($"{{{{{key}}}}}", value);

        return html;
    }

    public Task SendConfirmationEmailAsync(string email, string firstName, string confirmationUrl)
    {
        var subject = "Please confirm your email";
        var body = Render("ConfirmationEmail.html", new Dictionary<string, string>
        {
            { "FirstName", firstName },
            { "ConfirmationUrl", confirmationUrl }
        });

        return SendEmailAsync(email, subject, body);
    }

    public Task SendWelcomeEmailAsync(string? email, string firstName, string v1, string v2, string loginUrl)
    {
        var subject = "Welcome to AuthCore!";
        var body = Render("WelcomeEmail.html", new Dictionary<string, string>
        {
            { "FirstName", firstName },
            { "V1", v1 },
            { "V2", v2 },
            { "LoginUrl", loginUrl }
        });

        return SendEmailAsync(email ?? string.Empty, subject, body);
    }

    public Task SendPasswordResetEmailAsync(string? email, string firstName, string resetUrl)
    {
        var subject = "Password Reset Request";
        var body = Render("PasswordResetEmail.html", new Dictionary<string, string>
        {
            { "FirstName", firstName },
            { "ResetUrl", resetUrl }
        });

        return SendEmailAsync(email ?? string.Empty, subject, body);
    }
}
