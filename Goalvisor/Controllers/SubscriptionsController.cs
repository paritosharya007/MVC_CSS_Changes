using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Goalvisor.Helper;
using Goalvisor.Models;
using Goalvisor.Services.Subscriptions;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.Core;
using Goalvisor.ViewModels.DataTables.Base;
using System.Net;
using System.Threading.Tasks;

namespace Goalvisor.Controllers
{
    [Authorize]
    public class SubscriptionsController : Controller
    {
        private readonly ISubscriptionsService _subscriptionsService;

        public SubscriptionsController(ISubscriptionsService subscriptionsService)
        {
            _subscriptionsService = subscriptionsService;
        }

        public async Task<IActionResult> MySubscriptions(string subId)
        {
            if (!string.IsNullOrEmpty(subId))
            {
                try
                {
                    Stripe.StripeConfiguration.ApiKey = Helper.StripeUtil.SecretKey;
                    var service = new Stripe.SubscriptionService();
                    var subscriptionResponse = await service.CancelAsync(subId, null);
                    if (subscriptionResponse.Status == "canceled")
                    {
                        await _subscriptionsService.DeleteById(subId);
                    }
                }
                catch (System.Exception)
                {
                    return View();
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> PageData(DataTablesRequest request)
        {
            var result = await _subscriptionsService.DataTable(request);
            return new JsonResult(result);
        }

        [Authorize(Policy = RunTimeElements.AdministratorRole)]
        public async Task<IActionResult> Edit(int id)
        {
            Subscription subscription = null;

            subscription = await _subscriptionsService.GetById(id);
            if (subscription == null)
            {
                return BadRequest();
            }
            var vm = new SubscriptionVM
            {
                Duration = subscription.Duration,
                EndDate = subscription.EndDate,
                StartDate = subscription.StartDate,
                Id = subscription.Id,
                PackageName = subscription.Package.Name,
                UserName = subscription.User.FullName,
                Expired = subscription.Expired,
                PackageId = subscription.PackageId,
                UserId = subscription.UserId,
                Active = subscription.Active
            };
            return PartialView(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Subscription subscription)
        {
            var result = await _subscriptionsService.Update(subscription);

            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var result = await _subscriptionsService.DeleteById(id);

            return new JsonResult(result);
        }

        public JsonResult GetSubscriptionDetail(string subScriptionId)
        {
            try
            {
                if (string.IsNullOrEmpty(subScriptionId))
                {
                    return Json(new { ok = false, msg = "Invalid Subscription Id." });
                }

                Stripe.StripeConfiguration.ApiKey = StripeUtil.SecretKey;

                var service = new Stripe.SubscriptionService();
                var subscription = service.Get(subScriptionId);
                HttpStatusCode StatusCode = subscription.StripeResponse.StatusCode;
                if (StatusCode == HttpStatusCode.OK)
                {
                    return Json(new
                    {
                        BillingCycleAnchor = subscription.BillingCycleAnchor,
                        CollectionMethod = subscription.CollectionMethod,
                        CurrentPeriodStart = subscription.CurrentPeriodStart,
                        CurrentPeriodEnd = subscription.CurrentPeriodEnd,
                        Quantity = subscription.Quantity,
                        Status = subscription.Status,
                        Amount = subscription.Plan.Amount / 100,
                        BillingScheme = subscription.Plan.BillingScheme,
                        Created = subscription.Plan.Created,
                        Currency = subscription.Plan.Currency,
                        Interval = subscription.Plan.Interval,
                        Active = subscription.Plan.Active
                    });
                }
                return null;
            }
            catch (System.Exception ex)
            {
                return Json(new { ok = false, msg = "Exception occured." });
            }
        }

        public IActionResult Details(int id)
        {
            var subscription = _subscriptionsService.GetById(id);
            if (subscription == null)
            {
                return BadRequest();
            }
            return PartialView(subscription);
        }

        [Authorize(Policy = RunTimeElements.AdministratorRole)]
        public IActionResult Delete(int id)
        {
            _subscriptionsService.DeleteById(id);
            return Ok();
        }
    }
}