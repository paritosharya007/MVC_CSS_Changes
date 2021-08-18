using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Goalvisor.Data;
using Goalvisor.Models;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.DataTables.Base;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using System.IO;
using MimeKit;

namespace Goalvisor.Services.Email
{
    public class EmailService : IEmailService
    {
        private string host;
        private int port;
        private bool enableSSL;
        private string userName;
        private string password;
        private ApplicationDbContext _dbContext;
        public IConfiguration Configuration { get; }

        public EmailService(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            Configuration = configuration;
            _dbContext = dbContext;
            this.host = Configuration["EmailSender:Host"];
            this.port = Configuration.GetValue<int>("EmailSender:Port");
            this.enableSSL = Configuration.GetValue<bool>("EmailSender:EnableSSL");
            this.userName = Configuration["EmailSender:UserName"];
            this.password = Configuration["EmailSender:Password"];
        }

        public async Task<ServiceResult> AddOrUpdate(EmailTemplate emailtemplate)
        {
            var result = new ServiceResult();
            EmailTemplate toEdit;
            if (emailtemplate.Id <= 0)
            {
                toEdit = emailtemplate;
            }
            else
            {
                toEdit = await _dbContext.EmailTemplates.FirstOrDefaultAsync(p => p.Id == emailtemplate.Id);
            }
            if (toEdit == null)
            {
                result.Success = false;
                result.Message = "Template not found.";
            }
            toEdit.TemplateName = emailtemplate.TemplateName;
            toEdit.Subject = emailtemplate.Subject;
            toEdit.Body = emailtemplate.Body;
            if (emailtemplate.Id <= 0)
            {
                _dbContext.Add(toEdit);
            }
            await _dbContext.SaveChangesAsync();
            result.Success = true;
            result.Message = "Template successfully updated";
            return result;
        }

        public async Task<DataTableResponse> DataTable(DataTablesRequest request)
        {
            var total = _dbContext.EmailTemplates.Count();

            var filteredData = _dbContext.EmailTemplates.AsQueryable();
            if (!string.IsNullOrEmpty(request.Search.Value))
            {
                filteredData = filteredData.Where(item => item.TemplateName.Contains(request.Search.Value));
            }

            var count = filteredData.Count();

            if (request.Order != null)
            {
                var currentOrder = request.Order.FirstOrDefault();
                switch (currentOrder.Column)
                {
                    case 0:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.TemplateName) : filteredData.OrderBy(f => f.TemplateName);
                        break;

                    case 1:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.Subject) : filteredData.OrderBy(f => f.Subject);
                        break;

                    case 2:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.Body) : filteredData.OrderBy(f => f.Body);
                        break;
                }
            }

            var dataPage = await filteredData.Skip(request.Start).Take(request.Length).ToListAsync();

            var response = DataTableResponse.Create(total, count, dataPage);

            return response;
        }

        public async Task<ServiceResult> DeleteById(int TemplateId)
        {
            var result = new ServiceResult();
            if (_dbContext.EmailTemplates.Any(p => p.Id == TemplateId))
            {
                //  _dbContext.EmailTemplates.RemoveRange(await _dbContext.EmailTemplates.Where(p => p.PackageId == TemplateId).ToListAsync());
                _dbContext.EmailTemplates.RemoveRange(await _dbContext.EmailTemplates.FirstAsync(p => p.Id == TemplateId));
                try
                {
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    result.Success = false;
                    result.Message = e.Message;
                }
            }
            result.Message = "Template succesfully deleted.";
            return result;
        }

        public async Task<EmailTemplate> GetEmailBody(string templatename)
        {
            var template = await _dbContext.EmailTemplates.Where(t => t.TemplateName.Contains(templatename)).FirstOrDefaultAsync();
            return template;
        }

        public IQueryable<EmailTemplate> GetAll()
        {
            return _dbContext.EmailTemplates.AsQueryable();
        }

        public async Task<EmailTemplate> GetById(int TemplateId)
        {
            return await _dbContext.EmailTemplates.FirstOrDefaultAsync(p => p.Id == TemplateId);
        }

        public async Task<int> SendEmailAsync(string email, string subject, string htmlMessage)
        {
            int result = 0;
            try
            {
                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlMessage, null, MediaTypeNames.Text.Html);
                var client = new SmtpClient(host, port)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(userName, password),
                    EnableSsl = enableSSL
                };
                await client.SendMailAsync(
                   new MailMessage(userName, email, subject, htmlMessage)
                   {
                       IsBodyHtml = true,
                       AlternateViews = { htmlView },
                       From = new MailAddress(userName, " ScannerSuite"),
                       Subject = subject,
                       Body = htmlMessage,
                   }
               );
            }
            catch (Exception ex)
            { result = 0; }
            return result;
        }

        public int SendEmail(string email, string subject, string htmlMessage)
        {
            var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(userName, password),
                EnableSsl = enableSSL
            };
            client.Send(new MailMessage(userName, email, subject, htmlMessage) { IsBodyHtml = true });
            return 1;
        }

        public string ReadTemplateEmail(string body)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/" + "emailtemplate.html");
            var builder = new BodyBuilder();
            using (StreamReader SourceReader = System.IO.File.OpenText(path))
            {
                builder.HtmlBody = SourceReader.ReadToEnd();

            }
            string messageBody = string.Format(builder.HtmlBody,
                                                body);
            return messageBody;
        }
    }
}