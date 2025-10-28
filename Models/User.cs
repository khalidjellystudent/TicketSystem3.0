using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Models
{
    public class User
    {
        [Key]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string Name { get; set; }

        [DisplayName("Birth Date")]
        public DateOnly Birth_Date { get; set; }

        public string Sex { get; set; }

        [DisplayName("Blood Type")]
        public string Blood_Type { get; set; }

        public string Address { get; set; }

        [DisplayName("License Status")]
        public string License_Status { get; set; }

        public string Digree { get; set; }

        public string Role { get; set; } = "User";  // Default role


        // These properties are for account lockout brot force attacks
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }


    }
}
    