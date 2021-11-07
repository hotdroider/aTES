using aTES.Auth.Data;
using System.ComponentModel.DataAnnotations;

namespace aTES.Auth.Models.Account
{
    public class AddUser
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        public PopugRoles Role { get; set; } = PopugRoles.Developer;

        [Required]
        [MinLength(4, ErrorMessage = "The Password field must be a minimum of 4 characters")]
        public string Password { get; set; }
    }
}