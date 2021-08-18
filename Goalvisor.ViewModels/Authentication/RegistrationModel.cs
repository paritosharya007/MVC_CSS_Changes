using System.ComponentModel.DataAnnotations;

namespace Goalvisor.ViewModels
{
    public class RegistrationModel
    {
        [Required]
        public string Email { get; set; } = "";

        [Required]
        public string UserName { get; set; } = "";

        public string Name { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
        public int PackageId { get; set; }
        public string StripePackageId { get; set; }
        public string StripePriceId { get; set; }
        public string StripeSubId { get; set; }
        public string StripeCustomerId { get; set; }
        public int ReferralCode { get; set; }
    }
}