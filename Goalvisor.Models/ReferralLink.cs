using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goalvisor.Models
{
    public class ReferralLink
    {
        public int Id { get; set; }

        [Required]
        [StringLength(40, ErrorMessage = "Minimum 5 and Maximum 40 characters allowed", MinimumLength = 5)]
        public string Name { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Minimum 5 and Maximum 100 characters allowed.", MinimumLength = 5)]
        [RegularExpression(@"[a-zA-Z0-9]*$", ErrorMessage = "Only lower case a-z and numbers allowed!")]
        public string ReferralUrl { get; set; }

        public int UserId { get; set; }
        public DateTime GenerateDate { get; set; }

        [NotMapped]
        public string UserName { get; set; }

        [NotMapped]
        public int counter { get; set; }
    }
}