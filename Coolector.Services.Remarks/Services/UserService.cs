using System.Threading.Tasks;
using Coolector.Common.Domain;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Repositories;
using Coolector.Services.Users.Shared;
using NLog;

namespace Coolector.Services.Remarks.Services
{
    public class UserService : IUserService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task CreateIfNotFoundAsync(string userId, string name, string role)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasValue)
                return;

            Logger.Debug($"User not found, creating new one. userId: {userId}, name: {name}, role: {role}.");
            user = new User(userId, name, role);
            await _userRepository.AddAsync(user.Value);
        }

        public async Task UpdateNameAsync(string userId, string name)
        {
            Logger.Debug($"Update userName, userId:{userId}, name:{name}");
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
            {
                throw new ServiceException(OperationCodes.UserNotFound, 
                    $"User with id: {userId} does not exist");
            }

            user.Value.SetName(name);
            await _userRepository.UpdateAsync(user.Value);
        }
    }
}