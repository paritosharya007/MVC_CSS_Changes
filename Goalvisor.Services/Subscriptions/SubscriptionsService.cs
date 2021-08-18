using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Goalvisor.Data;
using Goalvisor.Models;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.Core;
using Goalvisor.ViewModels.DataTables.Base;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Goalvisor.Services.Subscriptions
{
    public class SubscriptionsService : ISubscriptionsService
    {
        private ApplicationDbContext _applicationDbContext;
        private UserManager<ApplicationUser> _userManager;

        public SubscriptionsService(ApplicationDbContext applicationDbContext,
            UserManager<ApplicationUser> userManager)
        {
            _applicationDbContext = applicationDbContext;
            _userManager = userManager;
        }

        public async Task<ServiceResult> Create(ApplicationUser user, Subscription subscription)
        {
            var result = new ServiceResult();
            using (var transaction = _applicationDbContext.Database.BeginTransaction())
            {
                // Subscription
                var newSubscription = new Subscription(); ;
                newSubscription.UserId = user.Id;
                newSubscription.PackageId = subscription.Id;
                newSubscription.StartDate = DateTime.Today;
                newSubscription.EndDate = DateTime.Today.AddDays(subscription.Duration);
                _applicationDbContext.Subscriptions.Add(newSubscription);
                if (!(await _userManager.IsInRoleAsync(user, RunTimeElements.SubscriberRole)))
                {
                    var identityResult = await _userManager.AddToRoleAsync(user, RunTimeElements.SubscriberRole);
                    if (!identityResult.Succeeded)
                    {
                        result.Success = false;
                        result.Message = "Error adding subscriber role!\n" + string.Join("\n", identityResult.Errors.Select(e => e.Description));
                        return result;
                    }
                }
                try
                {
                    await _applicationDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    result.Message = "Subscription successfully added!";
                    result.Id = newSubscription.Id;
                }
                catch (Exception e)
                {
                    result.Success = false;
                    result.Message = e.Message;
                    await transaction.RollbackAsync();
                }
            }
            return result;
        }

        public async Task<ServiceResult> Update(Subscription subcription)
        {
            var result = new ServiceResult();
            using (var transaction = _applicationDbContext.Database.BeginTransaction())
            {
                var subscriptionEntity = _applicationDbContext.Subscriptions.First(p => p.Id == subcription.Id);
                subscriptionEntity.StartDate = subcription.StartDate;
                subscriptionEntity.EndDate = subcription.EndDate;
                subscriptionEntity.Active = subcription.Active;
                var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == subscriptionEntity.UserId);
                if (!(await _userManager.IsInRoleAsync(user, RunTimeElements.SubscriberRole)))
                {
                    var identityResult = await _userManager.AddToRoleAsync(user, RunTimeElements.SubscriberRole);
                    if (!identityResult.Succeeded)
                    {
                        result.Success = false;
                        result.Message = "Error adding subscriber role !\n" + string.Join("\n", identityResult.Errors.Select(e => e.Description));
                        return result;
                    }
                }
                try
                {
                    await _applicationDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    result.Message = "Subscription successfully updated!";
                }
                catch (Exception e)
                {
                    result.Success = false;
                    result.Message = e.Message;
                    await transaction.RollbackAsync();
                }
            }
            return result;
        }

        public async Task<ServiceResult> Activate(Subscription subcription)
        {
            var result = new ServiceResult();
            using (var transaction = _applicationDbContext.Database.BeginTransaction())
            {
                var subscriptionEntity = _applicationDbContext.Subscriptions.First(p => p.Id == subcription.Id);

                subscriptionEntity.Active = subcription.Active;
                var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == subscriptionEntity.UserId);
                if (!(await _userManager.IsInRoleAsync(user, RunTimeElements.SubscriberRole)))
                {
                    var identityResult = await _userManager.AddToRoleAsync(user, RunTimeElements.SubscriberRole);
                    if (!identityResult.Succeeded)
                    {
                        result.Success = false;
                        result.Message = "Error adding subscriber role !\n" + string.Join("\n", identityResult.Errors.Select(e => e.Description));
                        return result;
                    }
                }
                try
                {
                    await _applicationDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    result.Message = "Subscription successfully updated!";
                }
                catch (Exception e)
                {
                    result.Success = false;
                    result.Message = e.Message;
                    await transaction.RollbackAsync();
                }
            }
            return result;
        }

        public async Task<ServiceResult> DeleteById(int subcriptionId)
        {
            var result = new ServiceResult();
            var subscriptionEntity = await _applicationDbContext.Subscriptions.FirstOrDefaultAsync(p => p.Id == subcriptionId);
            if (subscriptionEntity != null)
            {
                using (var transaction = _applicationDbContext.Database.BeginTransaction())
                {
                    try
                    {
                        _applicationDbContext.Remove(subscriptionEntity);
                        if (await _applicationDbContext.Subscriptions.CountAsync(s => subscriptionEntity.UserId == s.UserId && s.Id != subscriptionEntity.Id) > 0)
                        {
                            var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == subscriptionEntity.UserId);
                            if (await _userManager.IsInRoleAsync(user, RunTimeElements.SubscriberRole))
                            {
                                var identityResult = await _userManager.RemoveFromRoleAsync(user, RunTimeElements.SubscriberRole);
                                if (!identityResult.Succeeded)
                                {
                                    result.Success = false;
                                    result.Message = "Error removing subscriber role !\n" + string.Join("\n", identityResult.Errors.Select(e => e.Description));
                                    throw new Exception(result.Message);
                                }
                            }
                        }
                        await _applicationDbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        result.Message = "Subscription successfully removed!";
                    }
                    catch (Exception e)
                    {
                        result.Success = false;
                        result.Message = e.Message;
                        await transaction.RollbackAsync();
                    }
                }
            }
            else
            {
                result.Success = false;
                result.Message = "Subscription not found!";
            }
            return result;
        }

        public async Task<ServiceResult> DeleteById(string stripeSubscriptionId)
        {
            var result = new ServiceResult();

            var executionStrategy = _applicationDbContext.Database.CreateExecutionStrategy();
            var serviceResult = await executionStrategy.Execute(async () =>
            {
                var subscriptionEntity = await _applicationDbContext.Subscriptions.FirstOrDefaultAsync(p => p.StripeSubId == stripeSubscriptionId);
                if (subscriptionEntity != null)
                {
                    using (var transaction = _applicationDbContext.Database.BeginTransaction())
                    {
                        try
                        {
                            _applicationDbContext.Remove(subscriptionEntity);
                            if (await _applicationDbContext.Subscriptions.CountAsync(s => subscriptionEntity.UserId == s.UserId && s.Id != subscriptionEntity.Id) > 0)
                            {
                                var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == subscriptionEntity.UserId);
                                if (await _userManager.IsInRoleAsync(user, RunTimeElements.SubscriberRole))
                                {
                                    var identityResult = await _userManager.RemoveFromRoleAsync(user, RunTimeElements.SubscriberRole);
                                    if (!identityResult.Succeeded)
                                    {
                                        result.Success = false;
                                        result.Message = "Error removing subscriber role!\n" + string.Join("\n", identityResult.Errors.Select(e => e.Description));
                                        throw new Exception(result.Message);
                                    }
                                }
                            }
                            await _applicationDbContext.SaveChangesAsync();
                            await transaction.CommitAsync();
                            result.Message = "Subscription successfully removed";
                        }
                        catch (Exception e)
                        {
                            result.Success = false;
                            result.Message = e.Message;
                            await transaction.RollbackAsync();
                        }
                    }
                }
                else
                {
                    result.Success = false;
                    result.Message = "Subscription not found!";
                }
                return result;
            });
            return serviceResult;
        }

        public IQueryable<Subscription> GetAll()
        {
            return _applicationDbContext.Subscriptions
                .Include(s => s.Package)
                .Include(s => s.User)
                .AsQueryable();
        }

        public async Task<Subscription> GetById(int subcriptionId)
        {
            return await _applicationDbContext.Subscriptions
                .Include(s => s.Package)
                .Include(s => s.User)
                .FirstOrDefaultAsync(p => p.Id == subcriptionId);
        }

        public async Task<DataTableResponse> DataTable(DataTablesRequest request)
        {
            var total = await _applicationDbContext.Subscriptions.CountAsync();

            var filteredData = _applicationDbContext.Subscriptions
                .Include(s => s.Package)
                .Include(s => s.User)
                .AsQueryable();
            if (!string.IsNullOrEmpty(request.Search.Value))
            {
                filteredData = filteredData.Where(item =>
                item.Package.Name.Contains(request.Search.Value) ||
                item.User.FullName.Contains(request.Search.Value)
                );
            }

            var count = await filteredData.CountAsync();

            if (request.Order != null)
            {
                var currentOrder = request.Order.FirstOrDefault();
                switch (currentOrder.Column)
                {
                    case 0:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.Package.Name) : filteredData.OrderBy(f => f.Package.Name);
                        break;

                    case 1:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.User.FullName) : filteredData.OrderBy(f => f.User.FullName);
                        break;

                    case 2:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.StartDate) : filteredData.OrderBy(f => f.StartDate);
                        break;

                    case 3:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.EndDate) : filteredData.OrderBy(f => f.EndDate);
                        break;
                }
            }

            var temp = await filteredData.Skip(request.Start).Take(request.Length)
                .ToListAsync();
            var dataPage = temp
                .Select(s => new SubscriptionVM
                {
                    Id = s.Id,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Duration = s.Duration,
                    Expired = s.Expired,
                    PackageName = s.Package.Name,
                    PackageId = s.PackageId,
                    UserId = s.UserId,
                    UserName = s.User.FullName,
                    Active = s.Active
                })
                .ToList();

            var response = DataTableResponse.Create(total, count, dataPage);

            return response;
        }

        public async Task<Subscription> GetSubscriptionByUserId(int userId)
        {
            return await _applicationDbContext.Subscriptions
               .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<Subscription> GetById(string subcriptionId)
        {
            return await _applicationDbContext.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(p => p.StripeSubId == subcriptionId);
        }

        public async Task<ServiceResult> UpdateSubscription(Subscription subcription)
        {
            var result = new ServiceResult();
            var executionStrategy = _applicationDbContext.Database.CreateExecutionStrategy();
            var serviceResult = await executionStrategy.Execute(async () =>
            {
                using (var transaction = _applicationDbContext.Database.BeginTransaction())
                {
                    var subscriptionEntity = _applicationDbContext.Subscriptions.First(p => p.StripeSubId == subcription.StripeSubId);

                    subscriptionEntity.Name = subcription.Name;
                    subscriptionEntity.Description = subcription.Description;
                    subscriptionEntity.StripeProductId = subcription.StripeProductId;
                    subscriptionEntity.StripePriceId = subcription.StripePriceId;
                    subscriptionEntity.StartDate = subcription.StartDate;
                    subscriptionEntity.EndDate = subcription.EndDate;
                    subscriptionEntity.Active = subcription.Active;
                    var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == subscriptionEntity.UserId);
                    if (!(await _userManager.IsInRoleAsync(user, RunTimeElements.SubscriberRole)))
                    {
                        var identityResult = await _userManager.AddToRoleAsync(user, RunTimeElements.SubscriberRole);
                        if (!identityResult.Succeeded)
                        {
                            result.Success = false;
                            result.Message = "Error adding subscriber role !\n" + string.Join("\n", identityResult.Errors.Select(e => e.Description));
                            return result;
                        }
                    }
                    try
                    {
                        await _applicationDbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        result.Message = "Subscription successfully updated!";
                    }
                    catch (Exception e)
                    {
                        result.Success = false;
                        result.Message = e.Message;
                        await transaction.RollbackAsync();
                    }
                }
                return result;
            });

            return result;
        }

        public async Task<ServiceResult> CreateSubscription(ApplicationUser user, Subscription subscription)
        {
            var result = new ServiceResult();
            var executionStrategy = _applicationDbContext.Database.CreateExecutionStrategy();
            var serviceResult = await executionStrategy.Execute(async () =>
            {
                using (var transaction = _applicationDbContext.Database.BeginTransaction())
                {
                    //var newSub = new Subscription();
                    subscription.UserId = user.Id;
                    subscription.StartDate = DateTime.Today;
                    subscription.Active = true;
                    subscription.EndDate = DateTime.Today.AddDays(subscription.Duration);
                    _applicationDbContext.Subscriptions.Add(subscription);
                    if (!(await _userManager.IsInRoleAsync(user, RunTimeElements.SubscriberRole)))
                    {
                        var identityResult = await _userManager.AddToRoleAsync(user, RunTimeElements.SubscriberRole);
                        if (!identityResult.Succeeded)
                        {
                            result.Success = false;
                            result.Message = "Error adding subscriber role !\n" + string.Join("\n", identityResult.Errors.Select(e => e.Description));
                            return result;
                        }
                    }
                    try
                    {
                        await _applicationDbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        result.Message = "Subscription successfully added!";
                        result.Id = subscription.Id;
                    }
                    catch (Exception e)
                    {
                        result.Success = false;
                        result.Message = e.Message;
                        await transaction.RollbackAsync();
                    }
                }
                return result;
            });
            return serviceResult;
        }
    }
}