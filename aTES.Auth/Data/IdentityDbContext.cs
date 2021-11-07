using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace aTES.Auth.Data
{
    public class PopugsDbContext : IdentityDbContext<PopugUser>
    {
        public PopugsDbContext() : base() { }

        public PopugsDbContext(DbContextOptions<PopugsDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
    }
}
