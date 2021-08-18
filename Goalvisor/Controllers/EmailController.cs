using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Goalvisor.Models;
using Goalvisor.Services.Email;
using Goalvisor.ViewModels.Core;
using Goalvisor.ViewModels.DataTables.Base;
using System.Threading.Tasks;

namespace Goalvisor.Controllers
{
    [Authorize(Policy = RunTimeElements.AdministratorRole)]
    public class EmailController : Controller
    {
        private IHostingEnvironment Environment;
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public IActionResult List()
        {
            return View();
        }

        public IActionResult Template()
        {
            var templatevm = new EmailTemplate();
            templatevm.TemplateName = "Registration Success Template.";
            templatevm.Subject = "Congratulations! You have registered successfully.";
            templatevm.Body = "Dear {recipient}, \n Thanks for your registration. \n Please activate your account by using this link \n {activationlink} \n\n\n warm regards \n ScannerSuite Team";
            return View(templatevm);
        }

        [HttpPost]
        public async Task<IActionResult> PageData(DataTablesRequest request)
        {
            var result = await _emailService.DataTable(request);
            return new JsonResult(result);
        }

        public async Task<IActionResult> Edit(int id)
        {
            EmailTemplate model;
            if (id != 0)
            {
                model = await _emailService.GetById(id);
                if (model == null)
                {
                    return BadRequest();
                }
            }
            else
            {
                model = new EmailTemplate();
            }
            return PartialView(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddOrUpdate(EmailTemplate emailTemplate)
        {
            var result = await _emailService.AddOrUpdate(emailTemplate);
            TempData["SuccessMsg"] = result.Message;
            return RedirectToAction("List");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var result = await _emailService.DeleteById(id);
            TempData["SuccessMsg"] = result;
            return RedirectToAction("List");
        }
    }
}