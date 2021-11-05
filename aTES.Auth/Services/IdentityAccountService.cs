using aTES.Auth.Data;
using aTES.Auth.Kafka;
using aTES.Auth.Models.Account;
using aTES.Blazor;
using aTES.Common.Kafka;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace aTES.Auth.Services
{
    public class IdentityAccountService : IAccountService
    {
        private readonly UserManager<PopugUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly NavigationManager _navManager;

        private readonly IProducer _producer;

        public PopugUser User { get; private set; }

        public IdentityAccountService(SignInManager<PopugUser> signInManager,
            UserManager<PopugUser> userManager,
            RoleManager<IdentityRole> roleManager,
            NavigationManager navigationManager,
            IProducer producer)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _navManager = navigationManager;
            _producer = producer;
        }

        public async Task Initialize()
        {
            foreach (var role in Enum.GetValues<PopugRoles>())
            {
                var roleName = role.ToString();
                var r = await _roleManager.FindByNameAsync(roleName);
                if (r == null)
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
            }

            var admin = _userManager.Users.FirstOrDefault(u => u.UserName == "admin");
            if (admin == null)
                await Register(new AddUser()
                {
                    Username = "admin",
                    Password = "admin",
                    Email = "admin@popug.inc",
                    Role = PopugRoles.Admin
                });
        }

        public async Task Register(AddUser model)
        {
            var existing = _userManager.Users.FirstOrDefault(u => u.UserName == model.Username);
            if (existing != null)
                throw new Exception($"User {model.Username} already exists");

            var newUser = new PopugUser(model.Username)
            {
                Email = model.Email,
                Role = model.Role
            };

            var res = await _userManager.CreateAsync(newUser, model.Password);
            if (!res.Succeeded)
                throw new Exception(string.Join(Environment.NewLine, res.Errors.Select(r => r.Description)));

            res = await _userManager.AddToRoleAsync(newUser, model.Role.ToString());
            if (!res.Succeeded)
                throw new Exception(string.Join(Environment.NewLine, res.Errors.Select(r => r.Description)));

            res = await _userManager.AddClaimAsync(newUser, new Claim("PublicID", newUser.PublicKey));
            if (!res.Succeeded)
                throw new Exception(string.Join(Environment.NewLine, res.Errors.Select(r => r.Description)));

            await SendCUDAsync("Create", newUser);
        }

        public async Task<IList<PopugUser>> GetAll()
        {
            return await _userManager.Users
                .Where(u => !u.IsDeleted)
                .ToListAsync();
        }

        public Task<PopugUser> GetById(string id) => _userManager.FindByIdAsync(id);


        public Task<PopugUser> GetByName(string id) => _userManager.FindByNameAsync(id);

        public async Task Update(EditUser model)
        {
            var user = await GetById(model.Id);
            if (user == null)
                throw new Exception("User not found");

            user.Email = model.Email;
            user.UserName = model.Username;
            user.Role = model.Role;

            await _userManager.UpdateAsync(user);

            //1 popug - 1 role
            await _userManager.RemoveFromRolesAsync(user, Enum.GetValues<PopugRoles>().Select(r => r.ToString()));
            await _userManager.AddToRoleAsync(user, model.Role.ToString());

            await SendCUDAsync("Update", user);
        }

        public async Task Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            user.IsDeleted = true;

            await _userManager.UpdateAsync(user);

            await SendCUDAsync("Delete", user);
        }

        private Task SendCUDAsync(string messageType, PopugUser user)
        {
            return _producer.ProduceAsync(new
            {
                Type = messageType,
                At = DateTime.UtcNow,
                Account = new AccountMsg(user)
            }, "Accounts-stream");
        }
    }
}