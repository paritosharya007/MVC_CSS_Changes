using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Goalvisor.Models
{
    public class ReferralUser : IdentityUser<int>
    {
        public string FullName { get; set; }
        public IEnumerable<Subscription> Subscriptions { get; set; }
        public int ReferralCode { get; set; }
        public int ReferredBy { get; set; }
        public string ReferredByFullName { get; set; }
        public bool RevokeAccess { get; set; }
    }
}