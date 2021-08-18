using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Goalvisor.Models
{
    public class Subscription
    {
        [Key]
        public int Id { get; set; } = 0;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int PackageId { get; set; }
        public int UserId { get; set; }
        public bool Active { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string StripeProductId { get; set; }
        public string Interval { get; set; }
        public string StripePriceId { get; set; }
        public string StripeSubId { get; set; }
        public string StripeCustomerId { get; set; }

        [NotMapped]
        public bool Expired { get => StartDate.AddDays(Duration) >= EndDate; }

        [NotMapped]
        public int Duration { get => (EndDate - StartDate).Days; }

        [JsonIgnore]
        [ForeignKey(nameof(PackageId))]
        public Package Package { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }
    }
}