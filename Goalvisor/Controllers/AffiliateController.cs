using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Goalvisor.Helper;
using Goalvisor.Models;
using Goalvisor.Services.Affiliate;
using Goalvisor.ViewModels.Core;
using Goalvisor.ViewModels.DataTables.Base;
using System;
using System.Threading.Tasks;

namespace Goalvisor.Controllers
{
    [Authorize]
    public class AffiliateController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAffiliateService _affiliateService;
        public string baseUrl { get; set; }

        public AffiliateController(UserManager<ApplicationUser> userManager, IAffiliateService affiliateService)
        {
            _affiliateService = affiliateService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GenerateLink()
        {
            ViewBag.baseurl = PathHelper.FullyQualifiedApplicationPath(ControllerContext.HttpContext.Request) + "join?r=";
            return View();
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.baseUrl = PathHelper.FullyQualifiedApplicationPath(ControllerContext.HttpContext.Request) + "join?r=";
            ReferralLink referralLink;
            if (id != 0)
            {
                referralLink = await _affiliateService.GetById(id);
                string[] customUrl = referralLink.ReferralUrl.Split('=');
                referralLink.ReferralUrl = customUrl[1];
                if (referralLink == null)
                {
                    return BadRequest();
                }
            }
            else
            {
                referralLink = new ReferralLink();
            }
            return PartialView(referralLink);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var result = await _affiliateService.DeleteById(id);
            return new JsonResult(result);
        }

        public IActionResult MyReferralLinks()
        {
            return View();
        }

        public IActionResult ReferralDetails(int id = 0)
        {
            ViewData["linkId"] = id;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(ReferralLink referralLink)
        {
            baseUrl = PathHelper.FullyQualifiedApplicationPath(ControllerContext.HttpContext.Request) + "join?r=";
            ViewBag.baseUrl = baseUrl;
            referralLink.GenerateDate = DateTime.Now;
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                referralLink.UserId = user.Id;
                referralLink.ReferralUrl = baseUrl + referralLink.ReferralUrl;
                var checkExist = _affiliateService.GetByLink(referralLink.ReferralUrl);
                if (referralLink.Id <= 0)
                {
                    if (checkExist != null)
                    {
                        if (checkExist.Id > 0)
                        {
                            TempData["ErrorMsg"] = "The link " + referralLink.ReferralUrl + " already exists!";
                            return View("GenerateLink", referralLink);
                        }
                    }
                }
                else
                {
                    checkExist = await _affiliateService.GetByLinkAndId(referralLink.ReferralUrl, referralLink.Id);
                    if (checkExist != null)
                    {
                        if (checkExist.Id > 0)
                        {
                            TempData["ErrorMsg"] = "The link " + referralLink.ReferralUrl + " already exists!";

                            return View("Edit", referralLink);
                        }
                    }
                }
                var result = await _affiliateService.AddEditLink(referralLink);
                TempData["SuccessMsg"] = result.Message;

                if (User.IsInRole(RunTimeElements.AdministratorRole))
                {
                    return RedirectToAction("List");
                }
                else
                    return RedirectToAction("MyReferralLinks");
            }
            else
            {
                if (referralLink.Id > 0)
                {
                    return View("Edit", referralLink);
                }
                else
                    return View("GenerateLink", referralLink);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PageData(DataTablesRequest request)
        {
            var result = await _affiliateService.DataTable(request, 0);
            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> PageDataUser(DataTablesRequest request)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var result = await _affiliateService.DataTable(request, user.Id);
            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> GetReferLinkHistoryByLinkId(DataTablesRequest request, int id)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var result = await _affiliateService.GetReferLinkHistoryByLinkId(request, user.Id, id);
            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> GetByUserId()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var result = _affiliateService.GetByUserId(user.Id);
            return new JsonResult(result);
        }
    }
}