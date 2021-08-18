using System;

namespace Goalvisor.ViewModels
{
    public class ReferralLinkVm
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string ReferralUrl { get; set; }
        public DateTime GenerateDate { get; set; }

        public int counter { get; set; }
    }
}