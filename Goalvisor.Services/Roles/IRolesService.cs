using Microsoft.AspNetCore.Identity;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.DataTables.Base;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goalvisor.Services.Roles
{
    public interface IRolesService
    {
        Task<ServiceResult> Add(IdentityRole<int> role);

        Task<ServiceResult> Update(IdentityRole<int> role);

        Task<ServiceResult> Remove(int roleId);

        Task<DataTableResponse> DataTable(DataTablesRequest request);

        Task<IEnumerable<RoleVM>> GetAll();

        Task<ServiceResult> DeleteById(int userId);

        Task<IdentityRole<int>> GetById(int userId);
    }
}