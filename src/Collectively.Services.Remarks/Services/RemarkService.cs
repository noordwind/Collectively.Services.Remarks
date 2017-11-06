using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Extensions;
using Collectively.Services.Remarks.Queries;
using Collectively.Services.Remarks.Repositories;
using Collectively.Services.Remarks.Settings;
using Serilog;
using System.Net;
using Collectively.Common.Locations;

namespace Collectively.Services.Remarks.Services
{
    public class RemarkService : IRemarkService
    {
        private static readonly ILogger Logger = Log.Logger;
        private readonly IRemarkRepository _remarkRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupRemarkRepository _groupRemarkRepository;
        private readonly IRemarkPhotoService _remarkPhotoService;
        private readonly ITagManager _tagManager;
        private readonly GeneralSettings _settings;

        public RemarkService(IRemarkRepository remarkRepository, 
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            ITagRepository tagRepository,
            IGroupRepository groupRepository,
            IGroupRemarkRepository groupRemarkRepository,
            IRemarkPhotoService remarkPhotoService,
            ITagManager tagManager,
            GeneralSettings settings)
        {
            _remarkRepository = remarkRepository;
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
            _groupRepository = groupRepository;
            _groupRemarkRepository = groupRemarkRepository;
            _remarkPhotoService = remarkPhotoService;
            _tagManager = tagManager;
            _settings = settings;
        }

        public async Task<Maybe<Remark>> GetAsync(Guid id)
            => await _remarkRepository.GetByIdAsync(id);

        public async Task<Maybe<PagedResult<Remark>>> BrowseAsync(BrowseRemarks query)
            => await _remarkRepository.BrowseAsync(query);

        public async Task<Maybe<PagedResult<Category>>> BrowseCategoriesAsync(BrowseCategories query)
            => await _categoryRepository.BrowseAsync(query);

        public async Task<Maybe<PagedResult<Tag>>> BrowseTagsAsync(BrowseTags query)
            => await _tagRepository.BrowseAsync(query);

        public async Task ValidateEditorAccessOrFailAsync(Guid remarkId, string userId)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var user = await _userRepository.GetOrFailAsync(userId);
            if (user.HasAdministrativeRole)
            {
                return;
            }
            if (remark.Author.UserId != user.UserId)
            {
                throw new ServiceException(OperationCodes.UserNotAllowedToModifyRemark,
                    $"User with id: '{userId}' is not allowed " +
                    $"to modify the remark with id: '{remarkId}'.");
            }
        }

        public async Task CreateAsync(Guid id, string userId, string category, 
            Location location, string description = null, IEnumerable<Guid> tags = null,
            Guid? groupId = null, decimal? price = null, string currency = null,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            Logger.Debug($"Creating a remark, id: '{id}', user id: '{userId}', category: '{category}', " +
                         $"latitude: '{location.Latitude}', longitude: '{location.Longitude}'.");

            var remarkTags = await _tagManager.FindAsync(tags);
            if (remarkTags.HasNoValue || !remarkTags.Value.Any())
            {
                throw new ServiceException(OperationCodes.TagsNotProvided,
                    $"Tags were not provided for remark: '{id}'.");
            }
            var user = await _userRepository.GetOrFailAsync(userId);
            var remarkCategory = await _categoryRepository.GetByNameAsync(category);
            if (remarkCategory.HasNoValue)
            {
                throw new ServiceException(OperationCodes.CategoryNotFound,
                    $"Category: '{category}' does not exist!");
            }
            var encodedDescription = description.Empty() ? description : WebUtility.HtmlEncode(description);
            Group group = null;
            if (groupId != null)
            {
                group = await _groupRepository.GetOrFailAsync(groupId.Value);
            }
            var remark = new Remark(id, user, remarkCategory.Value, location, remarkTags.Value, encodedDescription, group);
            if (price.HasValue)
            {
                remark.SetOffering(Offering.Create(price.Value, currency, startDate, endDate));
            }
            await _remarkRepository.AddAsync(remark);
        }

        public async Task EditAsync(Guid remarkId, string userId, Guid? groupId, 
            string category, string description, Location location)
        {
            Logger.Debug($"Editing remark with id: '{remarkId}'.");
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var user = await _userRepository.GetOrFailAsync(userId);
            var state = remark.States.First();
            if (groupId != null)
            {
                var group = await _groupRepository.GetOrFailAsync(groupId.Value);
                remark.SetGroup(group);
            }
            if (category.NotEmpty())
            {
                var remarkCategory = await _categoryRepository.GetByNameAsync(category);
                if (remarkCategory.HasNoValue)
                {
                    throw new ServiceException(OperationCodes.CategoryNotFound,
                        $"Category: '{category}' does not exist!");
                }
                remark.SetCategory(remarkCategory.Value);
            }
            if (description.NotEmpty())
            {
                remark.SetDescription(WebUtility.HtmlEncode(description));
            }
            if (location != null)
            {
                remark.SetLocation(location);
            }
            remark.EditFirstState();
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task SetAvailableGroupsAsync(Guid remarkId, IEnumerable<Guid> groups)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            remark.SetAvailableGroups(groups);
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task UpdateUserNamesAsync(string userId, string name)
        {
            Logger.Debug($"Update user's remarks with new userName, userid: {userId}, userName: {name}");
            var user = await _userRepository.GetOrFailAsync(userId);
            await _remarkRepository.UpdateUserNamesAsync(userId, name);
        }

        public async Task DeleteAsync(Guid remarkId)
        {
            Logger.Debug($"Deleting remark with id: '{remarkId}'.");
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            if (remark.Photos.Any())
            {
                var photoNames = remark.Photos.Select(x => x.Name).ToArray();
                await _remarkPhotoService.RemovePhotosAsync(remark.Id, photoNames);
            }
            await _remarkRepository.DeleteAsync(remark);
            Logger.Debug($"Remark with id: '{remarkId}' was deleted.");
            if (remark.Group == null)
            {
                return;
            }
            var groupRemarks = await _groupRemarkRepository.GetAllAsync(remark.Group.Id);
            foreach (var groupRemark in groupRemarks)
            {
                groupRemark.DeleteRemark(remarkId);
            }
            await _groupRemarkRepository.UpdateManyAsync(groupRemarks);  
        }

        public async Task SubmitVoteAsync(Guid remarkId, string userId, bool positive, DateTime createdAt)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var user = await _userRepository.GetOrFailAsync(userId);
            if (positive)
            {
                remark.VotePositive(userId, createdAt);
            } 
            else
            {
                remark.VoteNegative(userId, createdAt);
            }
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task DeleteVoteAsync(Guid remarkId, string userId)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            remark.DeleteVote(userId);
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task AddFavoriteRemarkAsync(Guid remarkId, string userId)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var user = await _userRepository.GetOrFailAsync(userId);
            remark.AddUserFavorite(user);
            user.AddFavoriteRemark(remark);
            await _remarkRepository.UpdateAsync(remark);    
            await _userRepository.UpdateAsync(user);                  
        }

        public async Task DeleteFavoriteRemarkAsync(Guid remarkId, string userId)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var user = await _userRepository.GetOrFailAsync(userId);
            remark.RemoveUserFavorite(user);
            user.RemoveFavoriteRemark(remark);
            await _remarkRepository.UpdateAsync(remark);    
            await _userRepository.UpdateAsync(user);        
        }

        public async Task AssignToGroupAsync(Guid remarkId, Guid groupId, string userId)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var group = await _groupRepository.GetOrFailAsync(groupId);
            var user = await _userRepository.GetOrFailAsync(userId);
            ValidateGroupMemberRoleOrFail(group, user);
            remark.SetAssignedToGroupState(user, groupId);
            await _remarkRepository.UpdateAsync(remark);
            var groupRemarks = await _groupRemarkRepository.GetAllAsync(remarkId);
            foreach (var groupRemark in groupRemarks)
            {
                if (groupRemark.GroupId == groupId)
                {
                    groupRemark.Assign(remarkId);
                    continue;
                }
                groupRemark.Take(remarkId);
            }
            await _groupRemarkRepository.UpdateManyAsync(groupRemarks);  
        }

        public async Task DenyAssignmentToGroupAsync(Guid remarkId, Guid groupId, string userId)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var group = await _groupRepository.GetOrFailAsync(groupId);
            var user = await _userRepository.GetOrFailAsync(userId);
            ValidateGroupMemberRoleOrFail(group, user);
            var groupRemark = await _groupRemarkRepository.GetAsync(groupId);
            foreach (var remarkState in groupRemark.Value.Remarks)
            {
                if (remarkState.Id == groupId)
                {
                    remarkState.Deny();
                    break;
                }
            }
            await _groupRemarkRepository.UpdateAsync(groupRemark.Value);  
        }

        public async Task RemoveAssignmentAsync(Guid remarkId, string userId)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var user = await _userRepository.GetOrFailAsync(userId);
            if (remark.Group != null)
            {
                var group = await _groupRepository.GetOrFailAsync(remark.Group.Id);
                ValidateGroupMemberRoleOrFail(group, user);
            }
            remark.SetUnassignedState(user);
            await _remarkRepository.UpdateAsync(remark);
            if (remark.Group == null)
            {
                return;
            }
            var groupRemarks = await _groupRemarkRepository.GetAllAsync(remarkId);
            foreach (var groupRemark in groupRemarks)
            {
                if (groupRemark.GroupId == remark.Group.Id)
                {
                    groupRemark.Deny(remarkId);
                    continue;
                }
                groupRemark.Clear(remarkId);
            }
            await _groupRemarkRepository.UpdateManyAsync(groupRemarks); 
        }

        private void ValidateGroupMemberRoleOrFail(Group group, User user)
        {
            if (user.HasAdministrativeRole)
            {
                return;
            }
            var member = group.GetActiveMemberOrFail(user);
            if (member.HasAdministrativeRole)
            {
                return;
            }
            throw new ServiceException(OperationCodes.InsufficientGroupMemberRole,
                $"Group: '{group.Id}' member: '{user.Id}' does not have a " +
                $"suffiecient role.");                    
        }
    }
}