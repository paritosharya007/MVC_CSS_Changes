using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goalvisor.Models
{
    public class Package
    {
        [Key]
        public int Id { get; set; } = 0;

        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public int Duration { get; set; }
        public bool Active { get; set; }

        [NotMapped]
        public string ProductId { get; set; }

        [NotMapped]
        public string StripePriceId { get; set; }

        [NotMapped]
        public string Interval { get; set; }
    }
}