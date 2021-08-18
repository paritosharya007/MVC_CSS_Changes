using Goalvisor.Models;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.DataTables.Base;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goalvisor.Services.Users
{
    public interface IUserService
    {
        Task<ServiceResult> Add(UserVM user);

        Task<ServiceResult> Update(UserVM user);

        Task<ServiceResult> Remove(int id);

        Task<ServiceResult> MakeAdmin(int id);

        Task<ServiceResult> RevokeAdmin(int id);

        Task<DataTableResponse> DataTable(DataTablesRequest request);

        Task<DataTableResponse> Subscriptions(int id, DataTablesRequest request);

        Task<ServiceResult> UpdateRoles(int id, IEnumerable<string> roleNames);

        IQueryable<ApplicationUser> GetAll();

        Task<IEnumerable<string>> GetRoles(int id);

        Task<IEnumerable<string>> GetRoles(ApplicationUser user);

        Task<ServiceResult> DeleteById(int id);

        Task<ApplicationUser> GetById(int id);

        Task<ApplicationUser> GetByName(string name);

        Task<bool> HasActiveSubscription(string name);

        Task<int> GetIdByReferralCode(int referralCode);

        Task<bool> Revoke(int id);

        Task<bool> Restore(int id);

        Task<int> GenerateUniqueReferralCode(int codeLength);

        Task<bool> IsUniqueReferralCode(int uniqueVal);

        Task<bool> UpdateReferralCode(UserVM user);
    }
}