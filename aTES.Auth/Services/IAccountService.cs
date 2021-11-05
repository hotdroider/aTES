using aTES.Auth.Data;
using aTES.Auth.Models.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace aTES.Auth.Services
{
    public interface IAccountService
    {
        PopugUser User { get; }
        Task Initialize();
        Task Register(AddUser model);
        Task<IList<PopugUser>> GetAll();
        Task<PopugUser> GetById(string id);
        Task<PopugUser> GetByName(string id);
        Task Update(EditUser model);
        Task Delete(string id);
    }
}
