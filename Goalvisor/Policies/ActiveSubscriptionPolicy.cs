using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Goalvisor.Data;
using Goalvisor.Models;
using Goalvisor.ViewModels.Core;
using System;
using System.Threading.Tasks;

namespace Goalvisor.Policies
{
    public class ActiveSubscription : IAuthorizationRequirement
    {
    }

    public class ActiveSubscriptionPolicy : AuthorizationHandler<ActiveSubscription>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActiveSubscriptionPolicy(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActiveSubscription requirement)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            if (user == null)
            {
                context.Fail();
                return;
            }
            if ((await _dbContext.Subscriptions.AnyAsync(s =>
                            s.UserId == user.Id &&
                            s.StartDate <= DateTime.Now &&
                            s.EndDate >= DateTime.Now && s.Active)
                    && await _userManager.IsInRoleAsync(user, RunTimeElements.SubscriberRole))
                || await _userManager.IsInRoleAsync(user, RunTimeElements.AdministratorRole))
            {
                context.Succeed(requirement);
            }
        }
    }
}