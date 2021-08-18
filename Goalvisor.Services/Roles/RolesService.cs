using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Goalvisor.Data;
using Goalvisor.ViewModels;
using Goalvisor.ViewModels.Core;
using Goalvisor.ViewModels.DataTables.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goalvisor.Services.Roles
{
    public class RolesService : IRolesService
    {
        private ApplicationDbContext _applicationDbContext;
        private RoleManager<IdentityRole<int>> _roleManager;

        public RolesService(ApplicationDbContext applicationDbContext,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _applicationDbContext = applicationDbContext;
            _roleManager = roleManager;
            CheckMainRoles();
        }

        private void CheckMainRoles()
        {
            var roleToCheck = _applicationDbContext.Roles.Any(r => r.Name == RunTimeElements.AdministratorRole);

            if (roleToCheck == false)
            {
                _roleManager.CreateAsync(new IdentityRole<int>
                {
                    Name = RunTimeElements.AdministratorRole
                }).Wait();
            }

            roleToCheck = _applicationDbContext.Roles.Any(r => r.Name == RunTimeElements.SubscriberRole);
            if (roleToCheck == false)
            {
                _roleManager.CreateAsync(new IdentityRole<int>
                {
                    Name = RunTimeElements.SubscriberRole
                }).Wait();
            }

            roleToCheck = _applicationDbContext.Roles.Any(r => r.Name == RunTimeElements.UserRole);
            if (roleToCheck == false)
            {
                _roleManager.CreateAsync(new IdentityRole<int>
                {
                    Name = RunTimeElements.UserRole
                }).Wait();
            }
        }

        public async Task<ServiceResult> Add(IdentityRole<int> role)
        {
            var result = new ServiceResult();
            if (await _applicationDbContext.Roles.AnyAsync(r => r.Name == role.Name))
            {
                result.Success = false;
                result.Message = "Choose a different role name. You cannot add an existing role!";
                return result;
            }
            var identityResult = await _roleManager.CreateAsync(role);
            result.Success = identityResult.Succeeded;
            if (!result.Success)
            {
                result.Message = string.Join("\n", identityResult.Errors.Select(e => e.Description));
            }
            else
            {
                result.Message = "Role created successfully.";
            }

            return result;
        }

        public async Task<ServiceResult> Remove(int roleId)
        {
            var result = new ServiceResult();
            var role = await _applicationDbContext.Roles.FirstOrDefaultAsync(u => u.Id == roleId);
            if (checkForDeafaultRole(role.Name))
            {
                result.Success = false;
                result.Message = "Default roles cannot be removed!";
                return result;
            }

            var userRoles = await _applicationDbContext.UserRoles.Where(r => r.RoleId == roleId).ToListAsync();
            _applicationDbContext.UserRoles.RemoveRange(userRoles);
            _applicationDbContext.Roles.Remove(role);
            try
            {
                await _applicationDbContext.SaveChangesAsync();
                result.Success = true;
                result.Message = $"Role {role.Name} successfully deleted!";
            }
            catch (Exception e)
            {
                result.Success = false;
                result.Message = e.Message;
            }
            return result;
        }

        public async Task<DataTableResponse> DataTable(DataTablesRequest request)
        {
            var total = await _applicationDbContext.Roles.CountAsync();

            var filteredData = _applicationDbContext.Roles.AsQueryable();
            if (!string.IsNullOrEmpty(request.Search.Value))
            {
                filteredData = filteredData.Where(item =>
                    item.Name.Contains(request.Search.Value)
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
                }
            }

            var roles = await filteredData.Skip(request.Start).Take(request.Length)
                .Select(u => new RoleVM
                {
                    Id = u.Id,
                    RoleName = u.Name,
                    Editable = true,
                })
                .ToListAsync();
            roles.ForEach(t =>
            {
                t.Editable = !checkForDeafaultRole(t.RoleName);
            });

            var response = DataTableResponse.Create(total, count, roles);

            return response;
        }

        public async Task<IEnumerable<RoleVM>> GetAll()
        {
            return await _applicationDbContext.Roles
                .Select(u => new RoleVM
                {
                    Id = u.Id,
                    RoleName = u.Name,
                    Editable = true,
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> DeleteById(int userId)
        {
            var result = new ServiceResult();
            if (_applicationDbContext.Users.Any(p => p.Id == userId))
            {
                _applicationDbContext.Remove(await _applicationDbContext.UserRoles.Where(p => p.UserId == userId).ToListAsync());
                _applicationDbContext.Remove(await _applicationDbContext.Subscriptions.Where(p => p.UserId == userId).ToListAsync());
                _applicationDbContext.Remove(await _applicationDbContext.Users.FirstAsync(p => p.Id == userId));
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

        public async Task<IdentityRole<int>> GetById(int userId)
        {
            return await _applicationDbContext.Roles.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<ServiceResult> Update(IdentityRole<int> role)
        {
            var identityRoleEntity = await _applicationDbContext.Roles.FirstOrDefaultAsync(r => r.Id == role.Id);
            ServiceResult result = new ServiceResult();
            if (identityRoleEntity == null)
            {
                result.Success = false;
                result.Message = "Role does not exist !";
            }
            identityRoleEntity.Name = role.Name;
            identityRoleEntity.NormalizedName = role.Name.ToUpper();
            try
            {
                await _applicationDbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                result.Success = false;
                result.Message = "An error occured while saving the role. No data was saved";
            }
            result.Message = $"Role updated successfully to {role.Name}.";
            return result;
        }

        private bool checkForDeafaultRole(string roleName)
        {
            if (roleName == RunTimeElements.AdministratorRole
                || roleName == RunTimeElements.SubscriberRole
                || roleName == RunTimeElements.UserRole)
            {
                return true;
            }
            return false;
        }
    }
}