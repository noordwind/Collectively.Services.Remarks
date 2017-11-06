using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;

namespace Collectively.Services.Remarks.Repositories
{
    public class GroupRemarkRepository : IGroupRemarkRepository
    {
        private readonly IMongoDatabase _database;

        public GroupRemarkRepository(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<Maybe<GroupRemark>> GetAsync(Guid groupId)
            => await _database.GroupRemarks().GetAsync(groupId);

        public async Task<IEnumerable<GroupRemark>> GetAllAsync(Guid remarkId)
            => await _database.GroupRemarks().GetAllAsync(remarkId);

        public async Task AddAsync(GroupRemark groupRemark)
            => await _database.GroupRemarks().InsertOneAsync(groupRemark);

        public async Task AddRemarksAsync(Guid remarkId, IEnumerable<Guid> groupIds)
        {
            var groupRemarkState = GroupRemarkState.Create(remarkId);
            var filter = Builders<GroupRemark>.Filter.Where(x => groupIds.Contains(x.GroupId));
            var update = Builders<GroupRemark>.Update.AddToSet(x => x.Remarks, groupRemarkState);
            await _database.GroupRemarks().UpdateManyAsync(filter, update);
        }

        public async Task UpdateAsync(GroupRemark groupRemark)
            => await _database.GroupRemarks().ReplaceOneAsync(x => x.Id == groupRemark.Id, groupRemark);

        public async Task UpdateManyAsync(IEnumerable<GroupRemark> groupRemarks)
        {
            if (!groupRemarks.Any())
            {
                return;
            }
            var operations = groupRemarks.SelectMany(x => new WriteModel<GroupRemark>[]
            {
                new UpdateManyModel<GroupRemark>(
                    Builders<GroupRemark>.Filter.Eq(r => r.Id, x.Id),
                    Builders<GroupRemark>.Update.Set(r => r.Remarks, x.Remarks))
            }).ToArray();
            await _database.GroupRemarks().BulkWriteAsync(operations);           
        }
    }
}