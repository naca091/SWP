using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Project.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [Required(ErrorMessage = "Please enter customer name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please enter customer address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Please enter customer email")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone is required.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone must be 10 digits.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "Phone must be numeric.")]
        public string Phone { get; set; }
        public decimal Total { get; set; } // Thêm thuộc tính mới

    }
}
