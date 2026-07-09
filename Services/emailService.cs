using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Cryptography;
using MyApiBlya.Services;
public class EmailService : INotificationService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailService>_logger;
    public EmailService(IOptions<SmtpOptions> options,ILogger<EmailService>logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    public async Task SendAsync(string to, string subject,string message,CancellationToken token)
    {
        // _logger.LogInformation($"{_options.Email}");
        var Message = new MimeMessage();
        Message.From.Add(new MailboxAddress(_options.DisplayName,_options.Email));
        Message.To.Add(MailboxAddress.Parse(to));  
    Message.Subject= subject; 
    Message.Body = new TextPart("html")
    {
        Text = $"<h1>{message}</h1>"
    };
    using var client = new SmtpClient();
    try{ 
    
var host = _options.Host.Trim();
_logger.LogWarning($"{_options.Email}");
_logger.LogWarning($"{_options.Password}");


   await client.ConnectAsync(
        host,
        _options.Port,
SecureSocketOptions.StartTls
    );
    await client.AuthenticateAsync(
        _options.Email,
        _options.Password,token
    );
    await client.SendAsync(Message,token);
    await client.DisconnectAsync(true,token);
    
    return;}
    catch (Exception ex)
{
    Console.WriteLine(ex);
    throw;
}
    }
}