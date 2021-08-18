namespace Goalvisor.ViewModels
{
    public class UserVM
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public bool LockedOut { get; set; }
        public int ReferralCode { get; set; }
        public int ReferredBy { get; set; }
        public ReferralLinkVm ReferralLinkVm { get; set; }
        public SubscriptionVM SubscriptionVM { get; set; }
        public PackageVM PackageVM { get; set; }
    }
}