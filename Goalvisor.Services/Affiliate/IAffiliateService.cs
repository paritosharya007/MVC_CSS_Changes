using Goalvisor.Models;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.DataTables.Base;
using System.Linq;
using System.Threading.Tasks;

namespace Goalvisor.Services.Affiliate
{
    public interface IAffiliateService
    {
        Task<ServiceResult> AddEditLink(ReferralLink referalLink);

        Task<DataTableResponse> DataTable(DataTablesRequest request, int userid);

        Task<DataTableResponse> GetReferLinkHistoryByLinkId(DataTablesRequest request, int userid, int id);

        IQueryable<ReferralLink> GetAll();

        IQueryable<ReferralLink> GetByUserId(int userid);

        Task<ServiceResult> DeleteById(int TemplateId);

        Task<ReferralLink> GetById(int TemplateId);

        ReferralLink GetByLink(string link);

        Task<ReferralLink> GetByLinkAndId(string link, int id);
    }
}