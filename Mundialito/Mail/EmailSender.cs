using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using Mundialito.Configuration;

namespace Mundialito.Mail;

public class EmailSender : IEmailSender
{
     private readonly EmailClient _emailClient;
     private readonly Config _config;

    public EmailSender(IOptions<Config> config)
    {
        _config = config.Value;
    }

    public async void SendEmail(string toEmail, string subject, string messsage)
    {
        try
        {
            // This code demonstrates how to fetch your connection string
            // from an environment variable.
            string connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");
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
            Console.WriteLine($"Email operation id = {operationId}");
        }
        catch (RequestFailedException ex)
        {
            /// OperationID is contained in the exception message and can be used for troubleshooting purposes
            Console.WriteLine($"Email send operation failed with error code: {ex.ErrorCode}, message: {ex.Message}");
        }
    }
}