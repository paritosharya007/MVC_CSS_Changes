namespace Goalvisor.ViewModels
{
    public class UserViewModal
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Password { get; set; }
        public bool LockedOut { get; set; }
        public string ReferralCode { get; set; }
        public string ReferredBy { get; set; }
        public bool RevokeAccess { get; set; }
    }
}