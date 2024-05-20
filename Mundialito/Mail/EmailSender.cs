using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using Mundialito.Configuration;

namespace Mundialito.Mail;

public class EmailSender : IEmailSender
{
     private readonly Config _config;
     private readonly ILogger _logger;

    public EmailSender(ILogger<EmailSender> logger, IOptions<Config> config)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async void SendEmail(string toEmail, string subject, string messsage)
    {
        try
        {
            string connectionString = _config.EmailConnectionString;
            // TODO: Skip if empty
            _logger.LogInformation($"Will send mail to {toEmail} with connection string {connectionString}");
            EmailClient emailClient = new EmailClient(connectionString);
            EmailSendOperation emailSendOperation = await emailClient.SendAsync(
                Azure.WaitUntil.Completed,
                _config.FromAddress,
                toEmail,
                subject, null,
                messsage);
            EmailSendResult statusMonitor = emailSendOperation.Value;
            /// Get the OperationId so that it can be used for tracking the message for troubleshooting
            string operationId = emailSendOperation.Id;
            _logger.LogInformation($"Email operation id = {operationId}, status = {statusMonitor.Status}");
        }
        catch (RequestFailedException ex)
        {
            /// OperationID is contained in the exception message and can be used for troubleshooting purposes
            _logger.LogError($"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}");
        }
    }
}