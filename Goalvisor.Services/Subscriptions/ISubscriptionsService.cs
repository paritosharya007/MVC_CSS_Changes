using Goalvisor.Models;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.DataTables.Base;
using System.Linq;
using System.Threading.Tasks;

namespace Goalvisor.Services.Subscriptions
{
    public interface ISubscriptionsService
    {
        Task<ServiceResult> Update(Subscription subscription);

        Task<ServiceResult> UpdateSubscription(Subscription subscription);

        Task<ServiceResult> Activate(Subscription subscription);

        Task<ServiceResult> CreateSubscription(ApplicationUser user, Subscription subscription);

        Task<DataTableResponse> DataTable(DataTablesRequest request);

        IQueryable<Subscription> GetAll();

        Task<ServiceResult> DeleteById(int PackageId);

        Task<ServiceResult> DeleteById(string stripeSubscriptionId);

        Task<Subscription> GetById(int PackageId);

        Task<Subscription> GetById(string stripeSubscriptionId);

        Task<Subscription> GetSubscriptionByUserId(int userId);
    }
}