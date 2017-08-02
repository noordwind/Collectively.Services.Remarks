using System.Threading.Tasks;
using  Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Repositories
{
    public interface IUserRepository
    {
        Task<Maybe<User>> GetByUserIdAsync(string userId);
        Task<Maybe<User>> GetByNameAsync(string name);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(string userId);
    }
}