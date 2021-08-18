using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Goalvisor.Data;
using Goalvisor.Models;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.Core;
using Goalvisor.ViewModels.DataTables.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goalvisor.Services.Users
{
    public class UserService : IUserService
    {
        private ApplicationDbContext _applicationDbContext;
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;
        private IHttpContextAccessor _httpContextAccessor;

        public UserService(ApplicationDbContext applicationDbContext,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor,
             SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole<int>> roleManager)
        {
            _applicationDbContext = applicationDbContext;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
            CheckMainRoles(roleManager);
        }

        private void CheckMainRoles(RoleManager<IdentityRole<int>> roleManager)
        {
            var roleToCheck = _applicationDbContext.Roles.Any(r => r.Name == RunTimeElements.AdministratorRole);

            if (roleToCheck == false)
            {
                roleManager.CreateAsync(new IdentityRole<int>
                {
                    Name = RunTimeElements.AdministratorRole
                }).Wait();
            }

            roleToCheck = _applicationDbContext.Roles.Any(r => r.Name == RunTimeElements.SubscriberRole);
            if (roleToCheck == false)
            {
                roleManager.CreateAsync(new IdentityRole<int>
                {
                    Name = RunTimeElements.SubscriberRole
                }).Wait();
            }

            roleToCheck = _applicationDbContext.Roles.Any(r => r.Name == RunTimeElements.UserRole);
            if (roleToCheck == false)
            {
                roleManager.CreateAsync(new IdentityRole<int>
                {
                    Name = RunTimeElements.UserRole
                }).Wait();
            }
        }

        public async Task<ServiceResult> Add(UserVM user)
        {
            var result = new ServiceResult();
            if (await _applicationDbContext.Users.AnyAsync(u => u.UserName == user.UserName))
            {
                result.Success = false;
                result.Message = "Username already exists, please input a different username!";
                return result;
            }
            if (await _applicationDbContext.Users.AnyAsync(u => u.Email == user.Email))
            {
                result.Success = false;
                result.Message = "Email already exists, please input a different email!";
                return result;
            }

            using (var transaction = _applicationDbContext.Database.BeginTransaction())
            {
                //int referralUserId = _userManager.Users.Where(u => u.ReferralCode == user.ReferralCode).Select(u => u.Id).FirstOrDefault();

                var appUser = new ApplicationUser
                {
                    Email = user.Email,
                    FullName = user.FullName,
                    UserName = user.UserName,
                    ReferralCode = user.ReferralCode,
                    ReferredBy = user.ReferredBy
                };
                if (user.LockedOut)
                {
                    appUser.LockoutEnd = DateTime.Now.AddDays(30);
                }
                else
                {
                    appUser.LockoutEnd = null;
                }
                var identityResult = await _userManager.CreateAsync(appUser, user.Password);
                result.Success = identityResult.Succeeded;
                if (!result.Success)
                {
                    result.Message = string.Join("\n", identityResult.Errors.Select(e => e.Description));
                    await transaction.RollbackAsync();
                    return result;
                }
                identityResult = await _userManager.AddToRoleAsync(appUser, RunTimeElements.UserRole);
                result.Success = identityResult.Succeeded;
                if (!result.Success)
                {
                    result.Message = string.Join("\n", identityResult.Errors.Select(e => e.Description));
                    await transaction.RollbackAsync();
                    return result;
                }
                try
                {
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    result.Success = false;
                    result.Message = e.Message;
                }
            }
            result.Message = "User successfully added !";
            return result;
        }

        public async Task<ServiceResult> Update(UserVM user)
        {
            var result = new ServiceResult();
            if (await _applicationDbContext.Users.AnyAsync(u => u.UserName == user.UserName && u.Id != user.Id))
            {
                result.Success = false;
                result.Message = "Username already exists, please input a different username!";
                return result;
            }
            if (await _applicationDbContext.Users.AnyAsync(u => u.Email == user.Email && u.Id != user.Id))
            {
                result.Success = false;
                result.Message = "Email already exists, please input a different email!";
                return result;
            }

            var appUser = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (appUser == null)
            {
                result.Success = false;
                result.Message = "User not found !";
                return result;
            }
            var oldUserName = appUser.UserName;
            appUser.FullName = user.FullName;
            if (!string.IsNullOrEmpty(user.Password))
            {
                var password = new PasswordHasher<ApplicationUser>().HashPassword(appUser, user.Password);
                appUser.PasswordHash = password;
            }
            appUser.Email = user.Email;
            appUser.UserName = user.UserName;
            appUser.FullName = user.FullName;
            if (user.LockedOut)
            {
                appUser.LockoutEnd = DateTime.Now.AddDays(30);
            }
            else
            {
                appUser.LockoutEnd = null;
            }
            var identityResult = await _userManager.UpdateAsync(appUser);
            result.Success = identityResult.Succeeded;
            if (!result.Success)
            {
                result.Message = string.Join("\n", identityResult.Errors.Select(e => e.Description));
            }
            else
            {
                await _userManager.UpdateNormalizedEmailAsync(appUser);
                await _userManager.UpdateNormalizedUserNameAsync(appUser);
                await _userManager.UpdateSecurityStampAsync(appUser);
                if (_signInManager.IsSignedIn(_httpContextAccessor.HttpContext.User) && oldUserName == _httpContextAccessor.HttpContext.User.Identity.Name && !user.LockedOut)
                {
                    await _signInManager.RefreshSignInAsync(appUser);
                }
                result.Message = "User successfully updated !";
            }
            return result;
        }

        public async Task<ServiceResult> MakeAdmin(int userId)
        {
            var result = new ServiceResult();
            var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (!(await _userManager.IsInRoleAsync(user, RunTimeElements.AdministratorRole)))
            {
                var identityResult = await _userManager.AddToRoleAsync(user, RunTimeElements.AdministratorRole);
                result.Success = identityResult.Succeeded;
                if (!result.Success)
                {
                    result.Message = string.Join("\n", identityResult.Errors.Select(e => e.Description));
                }
            }

            return result;
        }

        public async Task<ServiceResult> RevokeAdmin(int id)
        {
            var result = new ServiceResult();
            var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if ((await _userManager.IsInRoleAsync(user, RunTimeElements.AdministratorRole)))
            {
                var identityResult = await _userManager.RemoveFromRoleAsync(user, RunTimeElements.AdministratorRole);
                result.Success = identityResult.Succeeded;
                if (!result.Success)
                {
                    result.Message = string.Join("\n", identityResult.Errors.Select(e => e.Description));
                }
            }
            result.Message = "User removed successfully.";
            return result;
        }

        public async Task<DataTableResponse> DataTable(DataTablesRequest request)
        {
            var total = await _applicationDbContext.Users.CountAsync();
            var filteredUsers = _applicationDbContext.Users.AsQueryable();

            List<ReferralUser> filteredDataWithReferredBy = new List<ReferralUser>();
            Dictionary<int, string> IdWithNames = new Dictionary<int, string>();

            foreach (var item in filteredUsers)
            {
                IdWithNames.Add(item.Id, item.FullName);
            }

            foreach (var item in filteredUsers)
            {
                ReferralUser user = new ReferralUser();
                user.Id = item.Id;
                user.FullName = item.FullName;
                user.Subscriptions = item.Subscriptions;
                user.Email = item.Email;
                user.ReferralCode = item.ReferralCode;
                user.ReferredBy = item.ReferredBy;
                user.ReferredByFullName = item.ReferredBy == 0 ? "N/A" : IdWithNames[item.ReferredBy];
                user.RevokeAccess = item.RevokeAccess;
                filteredDataWithReferredBy.Add(user);
            }

            if (!string.IsNullOrEmpty(request.Search.Value))
            {
                filteredDataWithReferredBy = filteredDataWithReferredBy.Where(item =>
                    item.FullName.Contains(request.Search.Value) ||
                    item.Email.Contains(request.Search.Value) ||
                    item.ReferredByFullName.Contains(request.Search.Value)
                ).ToList();
            }

            var count = filteredDataWithReferredBy.Count();

            var filteredData = filteredDataWithReferredBy.AsQueryable();

            if (request.Order != null)
            {
                var currentOrder = request.Order.FirstOrDefault();
                switch (currentOrder.Column)
                {
                    case 0:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.FullName) : filteredData.OrderBy(f => f.FullName);
                        break;

                    case 1:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.Email) : filteredData.OrderBy(f => f.Email);
                        break;

                    case 2:
                        filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.ReferredByFullName) : filteredData.OrderBy(f => f.ReferredByFullName);
                        break;
                }
            }

            var temp = filteredData.Skip(request.Start).Take(request.Length).ToList();
            var dataPage = temp
                .Select(u => new UserViewModal
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    ReferredBy = u.ReferredByFullName,
                    RevokeAccess = u.RevokeAccess
                })
                .ToList();

            var response = DataTableResponse.Create(total, count, dataPage);

            return response;
        }

        public IQueryable<ApplicationUser> GetAll()
        {
            return _applicationDbContext.Users.AsQueryable();
        }

        public async Task<ServiceResult> DeleteById(int id)
        {
            var result = new ServiceResult();
            if (_applicationDbContext.Users.Any(p => p.Id == id))
            {
                _applicationDbContext.Remove(await _applicationDbContext.UserRoles.Where(p => p.UserId == id).ToListAsync());
                _applicationDbContext.Remove(await _applicationDbContext.Subscriptions.Where(p => p.UserId == id).ToListAsync());
                _applicationDbContext.Remove(await _applicationDbContext.Users.FirstAsync(p => p.Id == id));
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
            return result;
        }

        public async Task<ApplicationUser> GetById(int id)
        {
            return await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<ServiceResult> Remove(int id)
        {
            ServiceResult result = new ServiceResult();
            var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == id); ;
            if (user == null)
            {
                result.Success = false;
                result.Message = "User not found!";
                return result;
            }
            var identityResult = await _userManager.DeleteAsync(user);
            result.Success = identityResult.Succeeded;
            if (!result.Success)
            {
                result.Message = string.Join("\n", identityResult.Errors.Select(e => e.Description));
            }
            return result;
        }

        public async Task<ServiceResult> UpdateRoles(int id, IEnumerable<string> roleNames)
        {
            ServiceResult result = new ServiceResult();
            var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                result.Success = false;
                result.Message = "User not found!";
                return result;
            }
            using (var transaction = _applicationDbContext.Database.BeginTransaction())
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var identityResult = await _userManager.RemoveFromRolesAsync(user, userRoles);
                result.Success = identityResult.Succeeded;
                if (!result.Success)
                {
                    result.Message = string.Join("\n", identityResult.Errors.Select(e => e.Description));
                    await transaction.RollbackAsync();
                    return result;
                }
                identityResult = await _userManager.AddToRolesAsync(user, roleNames);
                result.Success = identityResult.Succeeded;
                if (!result.Success)
                {
                    result.Message = string.Join("\n", identityResult.Errors.Select(e => e.Description));
                    await transaction.RollbackAsync();
                    return result;
                }
                await transaction.CommitAsync();
                result.Message = "User roles updates successfully.";
            }
            return result;
        }

        public async Task<IEnumerable<string>> GetRoles(int id)
        {
            var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            return await GetRoles(user);
        }

        public async Task<IEnumerable<string>> GetRoles(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<DataTableResponse> Subscriptions(int id, DataTablesRequest request)
        {
            try
            {
                var total = await _applicationDbContext.Subscriptions.CountAsync();

                var filteredData = _applicationDbContext.Subscriptions
                    .Where(s => s.UserId == id)
                    .AsQueryable();
                if (!string.IsNullOrEmpty(request.Search.Value))
                {
                    filteredData = filteredData.Where(item =>
                    item.Package.Name.Contains(request.Search.Value)
                    );
                }

                var count = await filteredData.CountAsync();

                if (request.Order != null)
                {
                    var currentOrder = request.Order.FirstOrDefault();
                    switch (currentOrder.Column)
                    {
                        case 0:
                            filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.Name) : filteredData.OrderBy(f => f.Name);
                            break;

                        case 1:
                            filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.StartDate) : filteredData.OrderBy(f => f.StartDate);
                            break;

                        case 2:
                            filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.EndDate) : filteredData.OrderBy(f => f.EndDate);
                            break;
                    }
                }

                var temp = await filteredData.Skip(request.Start).Take(request.Length).ToListAsync();
                var dataPage = temp
                    .Select(s => new SubscriptionVM
                    {
                        Id = s.Id,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        Duration = s.Duration,
                        Expired = s.Expired,
                        PackageName = s.Name,
                        StripeProducdId = s.StripeProductId,
                        StripeSubId = s.StripeSubId,
                        Active = s.Active
                    })
                    .ToList();

                var response = DataTableResponse.Create(total, count, dataPage);

                return response;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //public async Task<DataTableResponse> Subscriptions(int id, DataTablesRequest request)
        //{
        //    var total = await _applicationDbContext.Subscriptions.CountAsync();

        //    var filteredData = _applicationDbContext.Subscriptions
        //        .Include(s => s.Package)
        //        .Where(s => s.UserId == id)
        //        .AsQueryable();
        //    if (!string.IsNullOrEmpty(request.Search.Value))
        //    {
        //        filteredData = filteredData.Where(item =>
        //        item.Package.Name.Contains(request.Search.Value)
        //        );
        //    }

        //    var count = await filteredData.CountAsync();

        //    if (request.Order != null)
        //    {
        //        var currentOrder = request.Order.FirstOrDefault();
        //        switch (currentOrder.Column)
        //        {
        //            case 0:
        //                filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.Package.Name) : filteredData.OrderBy(f => f.Package.Name);
        //                break;
        //            case 1:
        //                filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.StartDate) : filteredData.OrderBy(f => f.StartDate);
        //                break;
        //            case 2:
        //                filteredData = currentOrder.Dir.ToUpper().Equals("DESC") ? filteredData.OrderByDescending(f => f.EndDate) : filteredData.OrderBy(f => f.EndDate);
        //                break;
        //        }
        //    }

        //    var temp = await filteredData.Skip(request.Start).Take(request.Length)
        //        .ToListAsync();
        //    var dataPage = temp
        //        .Select(s => new SubscriptionVM
        //        {
        //            Id = s.Id,
        //            StartDate = s.StartDate,
        //            EndDate = s.EndDate,
        //            Duration = s.Duration,
        //            Expired = s.Expired,
        //            PackageName = s.Package.Name,
        //            PackageId = s.PackageId,
        //            Active = s.Active
        //        })
        //        .ToList();

        //    var response = DataTableResponse.Create(total, count, dataPage);

        //    return response;
        //}

        public async Task<ApplicationUser> GetByName(string name)
        {
            return await _userManager.FindByNameAsync(name);
        }

        public async Task<bool> HasActiveSubscription(string name)
        {
            var user = await _applicationDbContext.Users.FirstAsync(u => u.UserName == name);
            return await _applicationDbContext.Subscriptions.AnyAsync(s => s.UserId == user.Id && s.Active && s.StartDate <= DateTime.Now && s.EndDate >= DateTime.Now);
        }

        public async Task<int> GetIdByReferralCode(int referralCode)
        {
            var user = await _applicationDbContext.Users.FirstAsync(u => u.ReferralCode == referralCode);
            if (user != null)
            {
                return user.Id;
            }
            else
            {
                return 0; //this would mean that ReferralCode was not found in the DB
            }
        }

        public async Task<bool> Revoke(int id)
        {
            try
            {
                var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
                //_applicationDbContext.Users.Update(user);
                user.RevokeAccess = true;
                await _applicationDbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> Restore(int id)
        {
            try
            {
                var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
                //_applicationDbContext.Users.Update(user);
                user.RevokeAccess = false;
                await _applicationDbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //--This method is not in use, as we would not have random default value for referral code--
        public async Task<int> GenerateUniqueReferralCode(int codeLength)
        {
            int uniqueVal = 0;
            List<int> referralCodes = new List<int>();

            referralCodes = await FetchAllReferralCodes();

            while (true)
            {
                //  uniqueVal = GetUniqueCode(codeLength);
                if (IsUniqueReferralCode(uniqueVal, referralCodes))
                    break;
            }
            return uniqueVal;
        }

        private bool IsUniqueReferralCode(int uniqueVal, List<int> referralCodes)
        {
            if (!referralCodes.Contains(uniqueVal))
                return true;
            else
                return false;
        }

        public async Task<bool> IsUniqueReferralCode(int uniqueVal)
        {
            List<int> referralCodes = new List<int>();
            referralCodes = await FetchAllReferralCodes();
            if (!referralCodes.Contains(uniqueVal))
                return true;
            else
                return false;
        }

        public async Task<bool> UpdateReferralCode(UserVM user)
        {
            try
            {
                var userRecord = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
                //_applicationDbContext.Users.Update(user);
                userRecord.ReferralCode = user.ReferralCode;
                await _applicationDbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string GetUniqueCode(int codeLength)
        {
            string _allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789";
            String returnValue = string.Empty;
            Random randNum = new Random();
            char[] chars = new char[codeLength];

            for (int i = 0; i < codeLength; i++)
            {
                chars[i] = _allowedChars[Convert.ToInt32((_allowedChars.Length - 1) * randNum.NextDouble())];
            }
            returnValue = new string(chars);

            return returnValue;
        }

        private async Task<List<int>> FetchAllReferralCodes()
        {
            List<int> referralCodes = new List<int>();
            referralCodes = await _applicationDbContext.Users.Select(r => r.ReferralCode).ToListAsync();
            return referralCodes;
        }
    }
}