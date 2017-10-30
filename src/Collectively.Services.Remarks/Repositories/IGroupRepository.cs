using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Repositories
{
    public interface IGroupRepository
    {
        Task<Maybe<Group>> GetAsync(Guid id);
        Task AddAsync(Group group);
        Task UpdateAsync(Group group);
        Task DeleteAsync(Guid id);
    }
}