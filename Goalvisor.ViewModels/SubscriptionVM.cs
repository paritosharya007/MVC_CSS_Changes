using System;

namespace Goalvisor.ViewModels
{
    public class SubscriptionVM
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PackageId { get; set; }
        public string PackageName { get; set; }
        public string UserName { get; set; }
        public int Duration { get; set; }
        public bool Expired { get; set; }
        public bool Active { get; set; }
        public string StripeProducdId { get; set; }
        public string StripeSubId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}