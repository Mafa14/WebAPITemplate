using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using WebAPITemplate.Database.Configurations;

namespace WebAPITemplate.Helpers.Messages
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Execute(email, subject, htmlMessage).Wait();
            return Task.FromResult(0);
        }

        public async Task Execute(string email, string subject, string htmlMessage)
        {
            // TODO: This entire class might change when we decide for an email service company
            var configurations = AppConfiguration.Configurations.EmailConfigurations;
            try
            {
                MailMessage mail = new MailMessage()
                {
                    From = new MailAddress(configurations.UsernameEmail, "Admin")
                };

                mail.To.Add(new MailAddress(email));

                mail.Subject = subject;
                mail.Body = htmlMessage;
                mail.IsBodyHtml = true;
                mail.Priority = MailPriority.High;

                using (SmtpClient smtp = new SmtpClient(configurations.SecondayDomain, configurations.SecondaryPort))
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(configurations.UsernameEmail, configurations.UsernamePassword);
                    smtp.EnableSsl = true;
                    await smtp.SendMailAsync(mail);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
