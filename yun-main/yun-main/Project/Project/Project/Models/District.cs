using System.ComponentModel.DataAnnotations;

namespace Project.Models
{
    public class District
    {
        [Key]
        public string code { get; set; }
        public string name { get; set; }
        public string name_en { get; set; }
        public string full_name { get; set; }
        public string full_name_en { get; set; }
        public string code_name { get; set; }
        public string province_code { get; set; }
        public int? AdministrativeUnitId { get; set; }
    }
}
