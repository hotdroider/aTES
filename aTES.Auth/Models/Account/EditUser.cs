using aTES.Auth.Data;
using System.ComponentModel.DataAnnotations;

namespace aTES.Auth.Models.Account
{
    public class EditUser
    {
        public string Id { get; set; } 

        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [MinLength(4, ErrorMessage = "The Password field must be a minimum of 4 characters")]
        public string Password { get; set; }

        public PopugRoles Role { get; set; }

        public EditUser() { }

        public EditUser(PopugUser user)
        {
            Id = user.Id;
            Email = user.Email;
            Username = user.UserName;
            Role = user.Role;
        }
    }
}