using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Goalvisor.Data;
using Goalvisor.Models;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.DataTables.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goalvisor.Services.Affiliate
{
    public class AffiliateService : IAffiliateService
    {
        private ApplicationDbContext _applicationDbContext;
        public  IConfiguration _configuration { get; }

        public AffiliateService(ApplicationDbContext applicationDbContext, IConfiguration configuration)
        {
            _configuration = configuration;
            _applicationDbContext = applicationDbContext;
        }

        public async Task<ServiceResult> AddEditLink(ReferralLink referralLink)
        {
            var result = new ServiceResult();
            ReferralLink referralLinkEntity = new ReferralLink();
            if (referralLink.Id <= 0)
            {
                referralLinkEntity = referralLink;
            }
            else
            {
                referralLinkEntity = await _applicationDbContext.ReferralLinks.FirstOrDefaultAsync(p => p.Id == referralLink.Id);
            }
            if (referralLinkEntity == null)
            {
                result.Success = false;
                result.Message = "Link not found.";
            }
            referralLinkEntity.Name = referralLink.Name;
            referralLinkEntity.ReferralUrl = referralLink.ReferralUrl;
            referralLinkEntity.UserId = referralLink.UserId;
            referralLinkEntity.GenerateDate = DateTime.Now;
            result.Message = "Link updated successfully!";
            if (referralLink.Id <= 0)
            {
                result.Message = "Link generated successfully!";
                _applicationDbContext.Add(referralLinkEntity);
            }

            await _applicationDbContext.SaveChangesAsync();
            result.Success = true;

            return result;
        }

        public async Task<DataTableResponse> DataTable(DataTablesRequest request, int userid)
        {
            var total = _applicationDbContext.ReferralLinks.Count();
            var filteredData = (from r in _applicationDbContext.ReferralLinks
                                join u in _applicationDbContext.Users on r.UserId equals u.Id
                                select new ReferralLink
                                {
                                    Id = r.Id,
                                    Name = r.Name,
                                    ReferralUrl = r.ReferralUrl,
                                    UserId = r.UserId,
                                    GenerateDate = r.GenerateDate.Date,
                                    UserName = u.UserName,
                                    counter = (_applicationDbContext.Users.Where(s => s.ReferralCode == r.Id).Count())
                                }
                               ).AsQueryable();
            if (userid > 0)
            {
                total = _applicationDbContext.ReferralLinks.AsQueryable().Where(x => x.UserId == userid).Count();
                filteredData = (from r in _applicationDbContext.ReferralLinks
                                join u in _applicationDbContext.Users on r.UserId equals u.Id
                                where r.UserId == userid
                                select new ReferralLink
                                {
                                    Id = r.Id,
                                    Name = r.Name,
                                    ReferralUrl = r.ReferralUrl,
                                    UserId = r.UserId,
                                    GenerateDate = r.GenerateDate.Date,
                                    UserName = u.UserName,
                                    counter = (_applicationDbContext.Users.Where(s => s.ReferralCode == r.Id).Count())
                                }
                               ).AsQueryable();
            }

            if (!string.IsNullOrEmpty(request.Search.Value))
            {
                filteredData = filteredData.Where(item => item.Name.Contains(request.Search.Value));
            }

            var count = filteredData.Count();

            if (request.Order != null)
            {
                var currentOrder = request.Order.FirstOrDefault();
                switch (currentOrder.Column)
                {
                    case 0:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.Name) : filteredData.OrderBy(f => f.Name);
                        break;

                    case 1:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.UserId) : filteredData.OrderBy(f => f.UserId);
                        break;

                    case 2:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.GenerateDate) : filteredData.OrderBy(f => f.GenerateDate);
                        break;
                }
            }

            var dataPage = await filteredData.Skip(request.Start).Take(request.Length).ToListAsync();

            var response = DataTableResponse.Create(total, count, dataPage);

            return response;
        }

        public async Task<ServiceResult> DeleteById(int referralid)
        {
            var result = new ServiceResult();
            if (referralid > 0)
            {
                var isexist = _applicationDbContext.Users.Where(p => p.ReferralCode == referralid).FirstOrDefault();
                if (isexist == null)
                {
                    var model = await _applicationDbContext.ReferralLinks.FirstAsync(p => p.Id == referralid);
                    _applicationDbContext.ReferralLinks.Remove(model);
                    try
                    {
                        await _applicationDbContext.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        result.Success = false;
                        result.Message = e.Message;
                    }
                }
                else
                {
                    result.Success = false;
                    result.Message = "A user has already registered using this link. It cannot be deleted!";
                }
            }
            result.Message = "Refferal link successfully deleted.";
            return result;
        }

        public IQueryable<ReferralLink> GetAll()
        {
            return _applicationDbContext.ReferralLinks.AsQueryable();
        }

        public async Task<ReferralLink> GetById(int referralid)
        {
            return await _applicationDbContext.ReferralLinks.FirstOrDefaultAsync(p => p.Id == referralid);
        }

        public ReferralLink GetByLink(string link)
        {
            return _applicationDbContext.ReferralLinks.Where(p => p.ReferralUrl == link).FirstOrDefault();
        }

        public async Task<DataTableResponse> GetReferLinkHistoryByLinkId(DataTablesRequest request, int userid, int id)
        {
            var filteredData = (from r in _applicationDbContext.ReferralLinks
                                where r.Id == id
                                join u in _applicationDbContext.Users on r.UserId equals u.Id
                                join s in _applicationDbContext.Subscriptions on u.Id equals s.UserId into subscript
                                from t in subscript.DefaultIfEmpty()
                                join p in _applicationDbContext.Packages on t.PackageId equals p.Id into Details
                                from m in Details.DefaultIfEmpty()
                                select new UserVM
                                {
                                    Id = u.Id,
                                    ReferralLinkVm = new ReferralLinkVm { Id = r.Id, Name = r.Name, ReferralUrl = r.ReferralUrl, GenerateDate = r.GenerateDate.Date, counter = (_applicationDbContext.Users.Where(s => s.ReferralCode == r.Id).Count()) },
                                    SubscriptionVM = new SubscriptionVM { Id = t.Id, PackageId = t.PackageId, StartDate = t.StartDate.Date, EndDate = t.EndDate.Date, Duration = t.Duration, Expired = t.Expired },
                                    PackageVM = new PackageVM { Id = m.Id, Name = m.Name, Description = m.Description },
                                    Email = u.Email,
                                    FullName = u.FullName,
                                    ReferredBy = u.ReferredBy,
                                    UserName = u.UserName,
                                }
                             ).AsQueryable();

            if (userid > 0)
            {
                filteredData = filteredData.Where(u => u.Id == userid);
            }

            var total = filteredData.Count();
            if (!string.IsNullOrEmpty(request.Search.Value))
            {
                filteredData = filteredData.Where(item => item.ReferralLinkVm.Name.Contains(request.Search.Value));
            }

            var count = filteredData.Count();

            if (request.Order != null)
            {
                var currentOrder = request.Order.FirstOrDefault();
                switch (currentOrder.Column)
                {
                    case 0:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.ReferralLinkVm.Name) : filteredData.OrderBy(f => f.ReferralLinkVm.Name);
                        break;

                    case 1:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.Id) : filteredData.OrderBy(f => f.Id);
                        break;

                    case 2:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.ReferralLinkVm.GenerateDate) : filteredData.OrderBy(f => f.ReferralLinkVm.GenerateDate);
                        break;
                }
            }

            var dataPage = await filteredData.Skip(request.Start).Take(request.Length).ToListAsync();

            var response = DataTableResponse.Create(total, count, dataPage);

            return response;
        }

        public async Task<ReferralLink> GetByLinkAndId(string link, int id)
        {
            return await _applicationDbContext.ReferralLinks.Where(p => p.ReferralUrl == link && p.Id != id).FirstOrDefaultAsync();
        }

        public IQueryable<ReferralLink> GetByUserId(int userid)
        {
            return _applicationDbContext.ReferralLinks.AsQueryable().Where(x => x.UserId == userid);
        }
    }
}