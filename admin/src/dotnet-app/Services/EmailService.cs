using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace QualityDoc.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string title, string messageBody, string actionUrl = null, string actionText = null)
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:Port"] ?? "2525");
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var senderName = _config["EmailSettings:SenderName"];
            var username = _config["EmailSettings:Username"];
            var password = _config["EmailSettings:Password"];

            string buttonHtml = !string.IsNullOrEmpty(actionUrl) 
                ? $"<div style='margin-top: 25px; text-align: center;'><a href='{actionUrl}' style='background-color: #4F46E5; color: white; padding: 12px 24px; text-decoration: none; font-weight: bold; border-radius: 8px; display: inline-block;'>{actionText}</a></div>" 
                : "";

            string bodyHtml = $@"
                <div style='background-color: #F8FAFC; padding: 40px 10px; font-family: sans-serif; color: #334155;'>
                    <div style='max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 16px; border: 1px solid #E2E8F0; overflow: hidden;'>
                        <div style='background: linear-gradient(135deg, #4F46E5 0%, #3730A3 100%); padding: 30px; text-align: center;'>
                            <h1 style='color: white; margin: 0; font-size: 22px; font-weight: 800;'>QualityDoc Polyglot</h1>
                            <p style='color: #C7D2FE; margin: 5px 0 0 0; font-size: 13px; font-weight: 600; text-transform: uppercase;'>Sistema de Gestión</p>
                        </div>
                        <div style='padding: 35px 30px;'>
                            <h2 style='margin-top: 0; color: #1E293B; font-size: 18px;'>{title}</h2>
                            <p style='font-size: 14px; line-height: 1.6;'>{messageBody}</p>
                            {buttonHtml}
                        </div>
                    </div>
                </div>";

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(senderEmail, senderName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = bodyHtml;
                message.IsBodyHtml = true;

                using (var client = new SmtpClient(smtpServer, port))
                {
                    // 🚀 ESTA LÍNEA ES OBLIGATORIA PARA GMAIL:
                    client.UseDefaultCredentials = false; 
                    
                    client.Credentials = new NetworkCredential(username, password);
                    client.EnableSsl = true;
                    await client.SendMailAsync(message);
                }
            }
        }
    }
}