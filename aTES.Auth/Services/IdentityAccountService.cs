using aTES.Auth.Data;
using aTES.Auth.Models.Account;
using aTES.Common;
using aTES.Common.Kafka;
using aTES.Events.SchemaRegistry;
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
        private readonly SchemaRegistry _schemaRegistry;

        private readonly IProducer _producer;

        private const string PRODUCER = "aTES.Auth";

        public PopugUser User { get; private set; }

        public IdentityAccountService(
            UserManager<PopugUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SchemaRegistry schemaRegistry,
            IProducer producer)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _producer = producer;
            _schemaRegistry = schemaRegistry;
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

            await SendCUDAsync("Accounts.Created", 1, newUser);
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

            await SendCUDAsync("Accounts.Updated", 1, user);
        }

        public async Task Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            user.IsDeleted = true;

            await _userManager.UpdateAsync(user);

            await SendCUDAsync("Accounts.Deleted", 1, user);
        }

        /// <summary>
        /// Build, validate and send CUD event for account
        /// </summary>
        private Task SendCUDAsync(string eventType, int version, PopugUser popug)
        {
            var evnt = new Event()
            {
                Name = eventType,
                Producer = PRODUCER,
                Version = version
            };

            evnt.Data = eventType switch
            {
                "Accounts.Created" => new
                {
                    Name = popug.UserName,
                    PublicKey = popug.PublicKey,
                    Email = popug.Email,
                    Role = popug.Role.ToString(),
                },
                "Accounts.Updated" => new
                {
                    Name = popug.UserName,
                    PublicKey = popug.PublicKey,
                    Email = popug.Email,
                    Role = popug.Role.ToString(),
                },
                "Accounts.Deleted" => new
                {
                    PublicKey = popug.PublicKey,
                },
                _ => throw new ArgumentException($"Unsupported event type {eventType}")
            };

            _schemaRegistry.ThrowIfValidationFails(evnt, eventType, version);

            return _producer.ProduceAsync(evnt, "Accounts-stream");
        }

    }
}