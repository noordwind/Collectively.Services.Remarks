using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;
using Collectively.Common.Files;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Extensions;
using Collectively.Services.Remarks.Repositories;
using Collectively.Services.Remarks.Settings;
using Serilog;

namespace Collectively.Services.Remarks.Services
{
    public class RemarkStateService : IRemarkStateService
    {
        private static readonly ILogger Logger = Log.Logger;
        private readonly IRemarkRepository _remarkRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRemarkPhotoService _remarkPhotoService;
        private readonly GeneralSettings _settings;

        public RemarkStateService(IRemarkRepository remarkRepository, 
            IUserRepository userRepository, 
            IRemarkPhotoService remarkPhotoService,
            GeneralSettings settings)
        {
            _remarkRepository = remarkRepository;
            _userRepository = userRepository;
            _remarkPhotoService = remarkPhotoService;
            _settings = settings;
        }

        public async Task ValidateRemoveStateAccessOrFailAsync(Guid remarkId, Guid stateId, string userId)
        {
            var user = await _userRepository.GetOrFailAsync(userId);
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var state = remark.GetState(stateId);
            if (state.HasNoValue)
            {
                throw new ServiceException(OperationCodes.StateNotFound, "Cannot find state." +
                    $" remarkId: {remarkId}, stateId: {stateId}");
            }

            if (state.Value.User.UserId != user.UserId)
            {
                throw new ServiceException(OperationCodes.UserNotAllowedToRemoveState,
                    $"User: {userId} is not allowed to remove state: {stateId}");
            }
        }

        public async Task ResolveAsync(Guid id, string userId, string description = null, Location location = null, 
            File photo = null, bool validateLocation = false)
         => await UpdateRemarkStateAsync(RemarkState.Names.Resolved, 
            (r,u,d) => r.SetResolvedState(u,d, location), id, userId, description, location, photo, validateLocation);

        public async Task AssignToGroupAsync(Guid id, string userId, Guid assignedGroupId, string description = null)
         => await UpdateRemarkStateAsync(RemarkState.Names.Assigned, 
            (r,u,d) => r.SetAssignedToGroupState(u, assignedGroupId, d), id, userId, description);

        public async Task AssignToUserAsync(Guid id, string userId, string assignedUserId, string description = null)
         => await UpdateRemarkStateAsync(RemarkState.Names.Assigned, 
            (r,u,d) => r.SetAssignedToUserState(u, assignedUserId, d), id, userId, description);

        public async Task RemoveAssignmentAsync(Guid id, string userId, string description = null)
         => await UpdateRemarkStateAsync(RemarkState.Names.Assigned, 
            (r,u,d) => r.SetUnassignedState(u, d), id, userId, description);

        public async Task ProcessAsync(Guid id, string userId, string description = null, Location location = null)
         => await UpdateRemarkStateAsync(RemarkState.Names.Processing, 
            (r,u,d) => r.SetProcessingState(u,d), id, userId, description, location);

        public async Task RenewAsync(Guid id, string userId, string description = null, Location location = null)
         => await UpdateRemarkStateAsync(RemarkState.Names.Renewed, 
            (r,u,d) => r.SetRenewedState(u,d), id, userId, description, location);

        public async Task CancelAsync(Guid id, string userId, string description = null, Location location = null)
         => await UpdateRemarkStateAsync(RemarkState.Names.Canceled, 
            (r,u,d) => r.SetCanceledState(u,d), id, userId, description, location);

        public async Task DeleteStateAsync(Guid remarkId, string userId, Guid stateId)
        {
            var user = await _userRepository.GetOrFailAsync(userId);
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var state = remark.GetState(stateId);
            if (state.Value.User.UserId != user.UserId)
            {
                throw new ServiceException(OperationCodes.UserNotAllowedToRemoveState,
                    $"User: {userId} is not allowed to remove state: {stateId}");
            }
            remark.RemoveState(stateId);
            await _remarkRepository.UpdateAsync(remark);
        }

        private async Task UpdateRemarkStateAsync(string state, Action<Remark,User,string> updateStateAction,
            Guid id, string userId, string description = null, Location location = null, 
            File photo = null, bool validateLocation = false)
        {
            var user = await _userRepository.GetOrFailAsync(userId);
            var remark = await _remarkRepository.GetOrFailAsync(id);
            if (location != null && validateLocation && remark.Location.IsInRange(location, _settings.AllowedDistance) == false)
            {
                throw new ServiceException(OperationCodes.DistanceBetweenUserAndRemarkIsTooBig,
                    $"The distance between user and remark: {id} is too big! " +
                    $"lat:{location.Latitude}, long:{location.Longitude}");
            }
            if (photo != null)
            {
                await _remarkPhotoService.UploadImagesWithDifferentSizesAsync(remark, userId, photo, state);
            }
            var encodedDescription = description.Empty() ? description : WebUtility.HtmlEncode(description);
            updateStateAction(remark, user, encodedDescription);
            await _remarkRepository.UpdateAsync(remark);              
        }
    }
}