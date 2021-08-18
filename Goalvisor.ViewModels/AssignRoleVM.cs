using System.Collections.Generic;

namespace Goalvisor.ViewModels
{
    public class AssignRoleVM
    {
        public int UserId { get; set; }
        public string userName { get; set; }

        public IEnumerable<RoleVM> Roles { get; set; }
    }
}