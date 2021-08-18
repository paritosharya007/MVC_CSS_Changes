using System;
using System.ComponentModel.DataAnnotations;

namespace Goalvisor.Models
{
    public class Log
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(25)]
        public string Status { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        /// <summary>
        /// This can be used for reference to any id e.g. customerid, productid etc
        /// </summary>
        [MaxLength(50)]
        public string OperationId { get; set; }

        [MaxLength(50)]
        public string LogType { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}