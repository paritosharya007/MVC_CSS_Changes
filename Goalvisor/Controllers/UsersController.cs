using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Goalvisor.Services.Roles;
using Goalvisor.Services.Subscriptions;
using Goalvisor.Services.Users;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.Core;
using Goalvisor.ViewModels.DataTables.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goalvisor.Controllers
{
    [Authorize(Policy = RunTimeElements.AdministratorRole)]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly IRolesService _rolesService;
        private readonly ISubscriptionsService _subscriptionsService;

        public UsersController(IUserService userService, IRolesService rolesService,  ISubscriptionsService subscriptionsService)
        {
            _userService = userService;
            _rolesService = rolesService;
            _subscriptionsService = subscriptionsService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> PageData(DataTablesRequest request)
        {
            return new JsonResult(await _userService.DataTable(request));
        }

        public async Task<IActionResult> AddOrUpdate(int id)
        {
            UserVM user = new UserVM();
            if (id > 0)
            {
                var temp = await _userService.GetById(id);
                user.Id = temp.Id;
                user.FullName = temp.FullName;
                user.Email = temp.Email;
                user.UserName = temp.UserName;
                user.LockedOut = temp.LockoutEnabled && temp.LockoutEnd >= DateTime.Now;
            }
            return PartialView(user);
        }

        [HttpPost]
        public async Task<IActionResult> AddOrUpdate(UserVM user)
        {
            ServiceResult result;
            if (user.Id <= 0)
            {
                result = await _userService.Add(user);
            }
            else
            {
                result = await _userService.Update(user);
            }
            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var result = await _userService.Remove(id);
            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<Boolean> Revoke(int id)
        {
            Boolean result = await _userService.Revoke(id);
            return result;
        }

        [HttpPost]
        public async Task<Boolean> Restore(int id)
        {
            Boolean result = await _userService.Restore(id);
            return result;
        }

        public async Task<IActionResult> AssignRoles(int id)
        {
            var user = await _userService.GetById(id);
            if (user == null)
            {
                return BadRequest();
            }
            var assignRoleVM = new AssignRoleVM
            {
                UserId = user.Id,
                userName = user.FullName,
                Roles = await _rolesService.GetAll()
            };
            var userRoles = await _userService.GetRoles(user);
            foreach (var item in assignRoleVM.Roles)
            {
                if (userRoles.Contains(item.RoleName))
                {
                    item.UserInRole = true;
                }
            }
            return PartialView(assignRoleVM);
        }

        [HttpPost]
        public async Task<IActionResult> AssignRoles(int id, IEnumerable<string> roleIds)
        {
            var result = await _userService.UpdateRoles(id, roleIds);
            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddSubscription(int id, int subscriptionId)
        {
            var user = await _userService.GetById(id);
            var subscription = await _subscriptionsService.GetById(subscriptionId);
            if (user == null || subscription == null)
            {
                return BadRequest();
            }
            var result = await _subscriptionsService.CreateSubscription(user, subscription);

            return new JsonResult(result);
        }
    }
}