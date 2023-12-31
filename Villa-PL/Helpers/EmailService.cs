using System.Net;
using System.Net.Mail;
using Villa_PL.Models;

namespace Villa_PL.Helpers
{
    public class EmailService
    {
        private readonly string _smtpHost = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUsername = "ahmedsamirsakr50@gmail.com";
        private readonly string _smtpPassword = "qsauierobyoboqzk";

        public void SendEmail(Email email)
        {
            using (var client = new SmtpClient(_smtpHost, _smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUsername),
                    Subject = email.Title,
                    Body = email.Body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(new MailAddress(email.To));

                client.Send(mailMessage);
            }
        }
    }
}
