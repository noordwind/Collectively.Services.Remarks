﻿using System.Threading.Tasks;
using Collectively.Common.Domain;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Extensions;
using Collectively.Services.Remarks.Repositories;
using NLog;

namespace Collectively.Services.Remarks.Services
{
    public class UserService : IUserService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task CreateIfNotFoundAsync(string userId, string name, 
            string role, string avatarUrl)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasValue)
            {
                return;
            }
            Logger.Debug($"Creating a new user: '{userId}', name: '{name}', role: '{role}'.");
            user = new User(userId, name, role);
            user.Value.SetAvatar(avatarUrl);
            await _userRepository.AddAsync(user.Value);
        }

        public async Task UpdateNameAsync(string userId, string name)
        {
            Logger.Debug($"Updating username for user: '{userId}' ['{name}'].");
            var user = await _userRepository.GetOrFailAsync(userId);
            user.SetName(name);
            await _userRepository.UpdateAsync(user);
        }

        public async Task UpdateAvatarAsync(string userId, string avatarUrl)
        {
            Logger.Debug($"Updating avatar for user: '{userId}' ['{avatarUrl}'].");
            var user = await _userRepository.GetOrFailAsync(userId);
            user.SetAvatar(avatarUrl);
            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteAsync(string userId)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
            {
                throw new ServiceException(OperationCodes.UserNotFound,
                    $"User with id: '{userId}' has not been found.");
            }
            await _userRepository.DeleteAsync(userId);
        }
    }
}