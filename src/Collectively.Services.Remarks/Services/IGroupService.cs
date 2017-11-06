using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collectively.Common.Locations;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    public interface IGroupService
    {
        Task CreateIfNotFoundAsync(Guid id, string name, bool isPublic, string state,
            string userId, IDictionary<string,ISet<string>> criteria,
            IEnumerable<Guid> tags, Guid? organizationId = null);
        Task ValidateIfRemarkCanBeCreatedOrFailAsync(Guid groupId, string userId, 
            double latitude, double longitude);
        Task ValidateIfRemarkCanBeAssignedOrFailAsync(Guid groupId, string userId,
            double latitude, double longitude);
        Task ValidateIfRemarkAssignmentCanBeRemovedOrFailAsync(Guid groupId, string userId);
        Task ValidateIfRemarkCanBeProcessedOrFailAsync(Guid groupId, string userId);
        Task ValidateIfRemarkCanBeResolvedOrFailAsync(Guid groupId, string userId);
        Task ValidateIfRemarkCanBeRenewedOrFailAsync(Guid groupId, string userId);
        Task ValidateIfRemarkCanBeCanceledOrFailAsync(Guid groupId, string userId);
        Task ValidateIfRemarkCanBeDeletedOrFailAsync(Guid groupId, string userId, Guid remarkId);
        Task ValidateIfRemarkCommentCanBeDeletedOrFailAsync(Guid groupId, string userId, Guid remarkId, Guid commentId);
        Task AddMemberAsync(Guid groupId, string memberId, string role);
        Task<IEnumerable<GroupLocation>> GetGroupLocationsAsync(LocationResponse location);
        Task<IEnumerable<GroupLocation>> FilterGroupLocationsByTagsAsync(IEnumerable<GroupLocation> groupLocations,
            IEnumerable<Guid> tags);
        Task AddRemarkToGroupsAsync(Guid remarkId, IEnumerable<Guid> groupIds);
    }
}