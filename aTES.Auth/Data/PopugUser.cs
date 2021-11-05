using Microsoft.AspNetCore.Identity;
using System;

namespace aTES.Auth.Data
{
    public class PopugUser : IdentityUser
    {
        public PopugUser(string name) 
            : base(name) { }

        public PopugUser()
            : base() { }

        /// <summary>
        /// aTES Popug public key
        /// </summary>
        public string PublicKey { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Role shorthand
        /// </summary>
        public PopugRoles Role { get; set; } = PopugRoles.Developer;

        /// <summary>
        /// Remover user
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}
