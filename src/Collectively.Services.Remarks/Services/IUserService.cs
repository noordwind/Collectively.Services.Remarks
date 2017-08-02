using System.Threading.Tasks;

namespace Collectively.Services.Remarks.Services
{
    public interface IUserService
    {
        Task CreateIfNotFoundAsync(string userId, string name, 
            string role, string state, string avatarUrl);
        Task UpdateNameAsync(string userId, string name);
        Task UpdateAvatarAsync(string userId, string avatarUrl);
        Task DeleteAsync(string userId, bool soft);
    }
}