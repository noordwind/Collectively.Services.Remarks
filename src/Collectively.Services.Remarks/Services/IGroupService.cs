using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Collectively.Services.Remarks.Services
{
    public interface IGroupService
    {
        Task CreateIfNotFoundAsync(Guid id, string name, bool isPublic, string state,
            string userId, IDictionary<string,string> criteria, IEnumerable<string> locations,
            Guid? organizationId = null);
        Task ValidateIfRemarkCanBeCreatedOrFailAsync(Guid groupId, string userId, 
            double latitude, double longitude);
    }
}