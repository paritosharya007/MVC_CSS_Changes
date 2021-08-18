using System.Collections.Generic;

namespace Goalvisor.ViewModels
{
    public class AddSubscriptionVM
    {
        public UserVM User { get; set; }
        public IEnumerable<PackageVM> Packages { get; set; }
    }
}