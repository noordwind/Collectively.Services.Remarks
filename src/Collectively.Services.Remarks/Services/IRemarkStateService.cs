using System;
using System.Threading.Tasks;
using Collectively.Common.Files;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    public interface IRemarkStateService
    {
        Task ValidateRemoveStateAccessOrFailAsync(Guid remarkId, Guid stateId, string userId);
        Task ResolveAsync(Guid id, string userId, string description = null, Location location = null, 
            File photo = null, bool validateLocation = false);
        Task AssignToGroupAsync(Guid id, string userId, Guid assignedGroupId, string description = null);
        Task AssignToUserAsync(Guid id, string userId, string assignedUserId, string description = null);
        Task RemoveAssignmentAsync(Guid id, string userId, string description = null);
        Task ProcessAsync(Guid id, string userId, string description = null, Location location = null);
        Task RenewAsync(Guid id, string userId, string description = null, Location location = null);
        Task CancelAsync(Guid id, string userId, string description = null, Location location = null);
        Task DeleteStateAsync(Guid remarkId, string userId, Guid stateId);
    }
}