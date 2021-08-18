using Goalvisor.Models;
using Goalvisor.Services.Email;
using Goalvisor.Services.Users;
using Goalvisor.ViewModels.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Goalvisor.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public HomeController(UserManager<ApplicationUser> userManager, IUserService userService, IEmailService emailService)
        {
            _userManager = userManager;
            _userService = userService;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            // TODO: Control view based on whether user is logged in or not

            ///To detect admin role and redirect admin home
            if (HttpContext.User.Identity.Name != null)
            {
                var user = await _userService.GetByName(HttpContext.User.Identity.Name);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    if (roles.Contains(RunTimeElements.AdministratorRole))
                    {
                        return Redirect("/Admin/");
                    }
                }
            }

            return View();
        }
    }
}