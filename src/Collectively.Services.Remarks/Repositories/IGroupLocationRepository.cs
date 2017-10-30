using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Repositories
{
    public interface IGroupLocationRepository
    {
        Task<Maybe<GroupLocation>> GetAsync(Guid groupId);
        Task AddAsync(GroupLocation groupLocality);
        Task<IEnumerable<GroupLocation>> GetAllWithLocationsAsync(IEnumerable<string> locations);          
    }
}