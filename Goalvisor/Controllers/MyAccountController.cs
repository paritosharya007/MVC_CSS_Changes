using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Goalvisor.Helper;
using Goalvisor.Services.Subscriptions;
using Goalvisor.Services.Users;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.DataTables.Base;
using Stripe;
using System;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Goalvisor.Controllers
{
    [Authorize]
    public class MyAccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ISubscriptionsService _subscriptionsService;

        public MyAccountController(IUserService userService, IConfiguration configuration, ISubscriptionsService subscriptionsService)
        {
            _userService = userService;
            _configuration = configuration;
            _subscriptionsService = subscriptionsService;
        }

        [HttpPost]
        public async Task<IActionResult> Subscriptions(int id, DataTablesRequest request)
        {
            var user = await _userService.GetByName(HttpContext.User.Identity.Name);
            var data = await _userService.Subscriptions(user.Id, request);
            return new JsonResult(data);
        }

        public async Task<IActionResult> Index(string subId)
        {
            ViewData["RootURL"] = _configuration.GetValue<string>("Root");

            var user = await _userService.GetByName(HttpContext.User.Identity.Name);
            if (!string.IsNullOrEmpty(subId))
            {
                StripeConfiguration.ApiKey = StripeUtil.SecretKey;
                var service = new SubscriptionService();
                var subscriptionResponse = await service.CancelAsync(subId, null);
                if (subscriptionResponse.Status == "canceled")
                {
                    await _subscriptionsService.DeleteById(subId);
                }
            }
            return View(user);
        }

        public async Task<IActionResult> UpdateInfo()
        {
            return PartialView(await _userService.GetByName(HttpContext.User.Identity.Name));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInfo(UserVM user)
        {
            var result = await _userService.Update(user);
            return new JsonResult(result);
        }

        public async Task<IActionResult> UpdateReferralId()
        {
            ViewData["RootURL"] = _configuration.GetValue<string>("Root");
            ViewData["codeLength"] = _configuration.GetValue<string>("ReferralCodeMaxLength");
            return PartialView(await _userService.GetByName(HttpContext.User.Identity.Name));
        }

        [HttpPost]
        public async Task<bool> UpdateReferralId(UserVM user)
        {
            bool isValid = ValidateRequest(user);
            if (isValid)
            {
                bool result = await _userService.UpdateReferralCode(user);
                return result;
            }
            else
            {
                return false;
            }
        }

        [HttpPost]
        public async Task<bool> IsUniqueReferralCode(int r)
        {
            return await _userService.IsUniqueReferralCode(r);
        }

        #region "Private Methods"

        private bool ValidateRequest(UserVM user)
        {
            //This is for server side validations.
            //Not using data annotation for validation as it may cause issue with existing data
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == Convert.ToInt32(userId))
                //if (isAlphaNumeric(user.ReferralCode) && user.ReferralCode.Length <= _configuration.GetValue<int>("ReferralCodeMaxLength"))
                //    return true;
                //else
                //    return false;
                return true;
            else
                return false;
        }

        private bool isAlphaNumeric(string referralCode)
        {
            Regex rg = new Regex(@"^[a-zA-Z0-9\s,]*$");
            return rg.IsMatch(referralCode);
        }

        #endregion "Private Methods"
    }
}