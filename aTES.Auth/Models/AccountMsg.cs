using aTES.Auth.Data;

namespace aTES.Auth.Kafka
{
    public class AccountMsg
    {
        public AccountMsg(PopugUser popug)
        {
            Name = popug.UserName;
            PublicKey = popug.PublicKey;
            Email = popug.Email;
            Role = popug.Role.ToString();

            if (popug.IsDeleted)
                IsDeleted = popug.IsDeleted;
        }

        public string Name { get; set; }

        public string PublicKey { get; set; }

        public string Email { get; set; }

        public string Role { get; set; }

        public bool? IsDeleted { get; set; }
    }
}
