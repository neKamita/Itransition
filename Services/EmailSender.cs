using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Itransition.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public EmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var smtp = GetSmtpClient();
        using var mail = CreateMailMessage(email, subject, htmlMessage);
        await smtp.SendMailAsync(mail);
    }

    private SmtpClient GetSmtpClient()
    {
        return new SmtpClient(_config["SmtpSettings:Server"], int.Parse(_config["SmtpSettings:Port"] ?? "587"))
        {
            Credentials = new NetworkCredential(_config["SmtpSettings:Username"], _config["SmtpSettings:Password"]),
            EnableSsl = true
        };
    }

    private MailMessage CreateMailMessage(string email, string subject, string body)
    {
        var sender = _config["SmtpSettings:SenderEmail"] ?? "noreply@itransition.com";
        return new MailMessage(sender, email, subject, body) { IsBodyHtml = true };
    }
}
