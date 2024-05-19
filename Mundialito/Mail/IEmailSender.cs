namespace Mundialito.Mail;

public interface IEmailSender
{
    void SendEmail(string toEmail, string subject, string message);
}