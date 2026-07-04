using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Cryptography;
using MyApiBlya.Services;
public class EmailService : INotificationService
{
    private readonly SmtpOptions _options;
    public EmailService(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }
    public async Task SendAsync(string to, string subject,string message)
    {
        var Message = new MimeMessage();
        Message.From.Add(new MailboxAddress(_options.DisplayName,_options.Email));
        Message.To.Add(MailboxAddress.Parse(to));  
    Message.Subject= subject; 
    Message.Body = new TextPart("html")
    {
        Text = $"<h1>{message}<h1/>"
    };
    using var client = new SmtpClient(); 
   await client.ConnectAsync(
        _options.Host,
        _options.Port,
SecureSocketOptions.StartTls
    );
    await client.AuthenticateAsync(
        _options.Email,
        _options.Password
    );
    await client.SendAsync(Message);
    await client.DisconnectAsync(true);
    client.Dispose();
    return;
    }
}