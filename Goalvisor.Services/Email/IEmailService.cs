using Goalvisor.Models;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.DataTables.Base;
using System.Linq;
using System.Threading.Tasks;

namespace Goalvisor.Services.Email
{
    public interface IEmailService
    {
        Task<ServiceResult> AddOrUpdate(EmailTemplate emailtemplate);

        Task<DataTableResponse> DataTable(DataTablesRequest request);

        IQueryable<EmailTemplate> GetAll();

        Task<ServiceResult> DeleteById(int TemplateId);

        Task<EmailTemplate> GetById(int TemplateId);

        Task<EmailTemplate> GetEmailBody(string templatename);

        Task<int> SendEmailAsync(string email, string subject, string htmlMessage);

        int SendEmail(string email, string subject, string htmlMessage);
        string ReadTemplateEmail(string body);
    }
}