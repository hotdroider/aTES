using System.ComponentModel.DataAnnotations;

namespace aTES.Auth.Models.Account
{
    public class Login
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public string ReturnTo { get; set; }
    }
}