using System.Threading.Tasks;

namespace QualityDoc.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string title, string messageBody, string actionUrl = null, string actionText = null);
    }
}