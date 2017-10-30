using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Repositories
{
    public interface IGroupRemarkRepository
    {
        Task<Maybe<GroupRemark>> GetAsync(Guid groupId);
        Task AddAsync(GroupRemark groupRemark);
        Task AddRemarksAsync(Guid remarkId, IEnumerable<Guid> groupIds);
        Task DeleteRemarksAsync(Guid remarkId, IEnumerable<Guid> groupIds);
    }
}