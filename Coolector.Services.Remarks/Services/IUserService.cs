using System.Threading.Tasks;

namespace Coolector.Services.Remarks.Services
{
    public interface IUserService
    {
        Task CreateIfNotFoundAsync(string userId, string name);
        Task UpdateNameAsync(string userId, string name);
    }
}