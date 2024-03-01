using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Project.Models
{
    public class NoteViewModel
    {
        [Required]
        public string NoteCode { get; set; }
        [Required]

        [Display(Name ="Created's Name: ")]
        public string UserName { get; set; }

        [Display(Name = "Customer's Name: ")]
        [Required]
        public string Customer { get; set; }

        public string Phone { get; set; }
        [Required]

        public string AddressCustomer { get; set; }
        [Required]
        public string Reason { get; set; }


        public List<NoteProductViewModel> Products { get; set; }

    }

    public class NoteProductViewModel
    {
        public int ProductID { get; set; }
        public int StockOut { get; set; }
    }
}
