using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Goalvisor.Services.Roles;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.Core;
using Goalvisor.ViewModels.DataTables.Base;
using System.Threading.Tasks;

namespace Goalvisor.Controllers
{
    [Authorize(Policy = RunTimeElements.AdministratorRole)]
    public class RolesController : Controller
    {
        private IRolesService _rolesSerice;

        public RolesController(IRolesService rolesSerice)
        {
            _rolesSerice = rolesSerice;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> PageData(DataTablesRequest request)
        {
            return new JsonResult(await _rolesSerice.DataTable(request));
        }

        public async Task<IActionResult> AddOrUpdate(int id)
        {
            IdentityRole<int> role;
            if (id > 0)
            {
                role = await _rolesSerice.GetById(id);
                if (role == null)
                {
                    return BadRequest();
                }
            }
            else
            {
                role = new IdentityRole<int>();
            }
            return PartialView(role);
        }

        [HttpPost]
        public async Task<IActionResult> AddOrUpdate(IdentityRole<int> role)
        {
            ServiceResult result;
            if (role.Id == 0)
            {
                result = await _rolesSerice.Add(role);
            }
            else
            {
                result = await _rolesSerice.Update(role);
            }

            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var result = await _rolesSerice.Remove(id);
            return new JsonResult(result);
        }
    }
}