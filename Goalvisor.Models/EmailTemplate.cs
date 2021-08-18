using System.ComponentModel.DataAnnotations;

namespace Goalvisor.Models
{
    public class EmailTemplate
    {
        public int Id { get; set; }
        public string TemplateName { get; set; }
        public string Subject { get; set; }

        [DataType(DataType.MultilineText)]
        public string Body { get; set; }
    }
}