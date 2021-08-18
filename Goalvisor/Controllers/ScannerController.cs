using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Goalvisor.ViewModels.Core;

namespace Goalvisor.Controllers
{
    [Authorize(Policy = RunTimeElements.SubscriberOrAdminPolicy)]
    public class ScannerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}