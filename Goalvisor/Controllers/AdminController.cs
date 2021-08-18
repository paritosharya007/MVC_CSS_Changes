using Goalvisor.Services.Subscriptions;
using Goalvisor.Services.Users;
using Goalvisor.ViewModels.Core;
using Goalvisor.ViewModels.DataTables.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Goalvisor.Controllers
{
    [Authorize(Policy = RunTimeElements.AdministratorRole)]
    public class AdminController : Controller
    {
        private readonly ISubscriptionsService _subscriptionsService;
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        public AdminController(ISubscriptionsService subscriptionsService, IUserService userService, IConfiguration configuration)
        {
            _subscriptionsService = subscriptionsService;
            _userService = userService;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> MyAccount()
        {
            var user = await _userService.GetByName(HttpContext.User.Identity.Name);
            ViewData["RootURL"] = _configuration.GetValue<string>("Root");
            return View(user);
        }

        public IActionResult Subscriptions()
        {
            return View();
        }

        public IActionResult Packages()
        {
            return View();
        }

        public IActionResult Affiliates()
        {
            return View();
        }

        public IActionResult Scanner()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PageData(DataTablesRequest request)
        {
            var result = await _subscriptionsService.DataTable(request);
            return new JsonResult(result);
        }
    }
}