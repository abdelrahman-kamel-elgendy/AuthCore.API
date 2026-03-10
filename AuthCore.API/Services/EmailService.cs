using System.Net;
using System.Net.Mail;
using AuthCore.API.Services.Interfaces;

namespace AuthCore.API.Services;

public class EmailService(IConfiguration config) : IEmailService
{
    private readonly IConfiguration _config = config;

    public async Task SendEmailAsync(string toEmail, string subject, string body)
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

    public static string Render(string templateName, Dictionary<string, string> values)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", templateName);
        var html = File.ReadAllText(templatePath);

        foreach (var (key, value) in values)
            html = html.Replace($"{{{{{key}}}}}", value);

        return html;
    }
}
