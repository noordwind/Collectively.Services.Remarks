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
using NLog;
using System.Net;

namespace Collectively.Services.Remarks.Services
{
    public class RemarkService : IRemarkService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IRemarkRepository _remarkRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IRemarkPhotoService _remarkPhotoService;
        private readonly IUserRepository _userRepository;
        private readonly GeneralSettings _settings;

        public RemarkService(IRemarkRepository remarkRepository, 
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            ITagRepository tagRepository,
            IRemarkPhotoService remarkPhotoService,
            GeneralSettings settings)
        {
            _remarkRepository = remarkRepository;
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
            _remarkPhotoService = remarkPhotoService;
            _userRepository = userRepository;
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
            if (user.Role == "moderator" || user.Role == "administrator")
            {
                return;
            }
            if (remark.Author.UserId != user.UserId)
            {
                throw new ServiceException(OperationCodes.UserNotAllowedToModifyRemark,
                    $"User with id: '{userId}' is not allowed" +
                    $"to modify the remark with id: '{remarkId}'.");
            }
        }

        public async Task CreateAsync(Guid id, string userId, string category, 
            Location location, string description = null, IEnumerable<string> tags = null)
        {
            Logger.Debug($"Create remark, id:{id}, userId: {userId}, category: {category}, " +
                         $"latitude: {location.Latitude}, longitude: {location.Longitude}.");
            var user = await _userRepository.GetOrFailAsync(userId);
            var remarkCategory = await _categoryRepository.GetByNameAsync(category);
            if (remarkCategory.HasNoValue)
            {
                throw new ServiceException(OperationCodes.CategoryNotFound,
                    $"Category: '{userId}' does not exist!");
            }
            var encodedDescription = description.Empty() ? description : WebUtility.HtmlEncode(description);
            var remark = new Remark(id, user, remarkCategory.Value, location, encodedDescription);
            if (tags == null || !tags.Any())
            {
                await _remarkRepository.AddAsync(remark);
                return;
            }

            var availableTags = await _tagRepository.BrowseAsync(new BrowseTags
            {
                Results = 1000
            });
            if(availableTags.HasValue)
            {
                var selectedTags = availableTags.Value.Items
                                    .Select(x => x.Name)
                                    .Intersect(tags);

                foreach (var tag in selectedTags)
                {
                    remark.AddTag(tag);
                }
            }
            await _remarkRepository.AddAsync(remark);
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
            await _remarkPhotoService.RemovePhotosAsync(remark.Id);
            await _remarkRepository.DeleteAsync(remark);
            Logger.Debug($"Remark with id: '{remarkId}' was deleted.");
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
    }
}