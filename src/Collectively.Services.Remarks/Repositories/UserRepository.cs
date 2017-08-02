using System;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;

namespace Collectively.Services.Remarks.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoDatabase _database;

        public UserRepository(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<Maybe<User>> GetByUserIdAsync(string userId)
            => await _database.Users().GetByUserIdAsync(userId);

        public async Task<Maybe<User>> GetByNameAsync(string name)
            => await _database.Users().GetByNameAsync(name);

        public async Task AddAsync(User user)
            => await _database.Users().InsertOneAsync(user);

        public async Task UpdateAsync(User user)
            => await _database.Users().ReplaceOneAsync(x => x.Id == user.Id, user);

        public async Task DeleteAsync(string userId)
            => await _database.Users().DeleteOneAsync(x => x.UserId == userId);
    }
}