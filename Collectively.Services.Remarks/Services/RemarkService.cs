using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Queries;
using Collectively.Services.Remarks.Repositories;
using Collectively.Services.Remarks.Settings;
using NLog;

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
            var remark = await _remarkRepository.GetByIdAsync(remarkId);
            if (remark.HasNoValue)
            {
                throw new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: '{remarkId}' does not exist!");
            }

            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
            {
                throw new ServiceException(OperationCodes.UserNotFound,
                    $"User with id: '{userId}' does not exist!");
            }
            if (user.Value.Role == "moderator" || user.Value.Role == "administrator")
            {
                return;
            }
            if (remark.Value.Author.UserId != user.Value.UserId)
            {
                throw new ServiceException(OperationCodes.UserNotAllowedToModifyRemark,
                    $"User with id: '{userId}' is not allowed" +
                    $"to modify the remark with id: '{remarkId}'.!");
            }
        }

        public async Task CreateAsync(Guid id, string userId, string category, 
            Location location, string description = null, IEnumerable<string> tags = null)
        {
            Logger.Debug($"Create remark, id:{id}, userId: {userId}, category: {category}, " +
                         $"latitude: {location.Latitude}, longitude: {location.Longitude}.");
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
            {
                throw new ServiceException(OperationCodes.UserNotFound,
                    $"User with id: '{userId}' does not exist!");
            }

            var remarkCategory = await _categoryRepository.GetByNameAsync(category);
            if (remarkCategory.HasNoValue)
            {
                throw new ServiceException(OperationCodes.CategoryNotFound,
                    $"Category: '{userId}' does not exist!");
            }

            var remark = new Remark(id, user.Value, remarkCategory.Value, location, description);
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
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
            {
                throw new ServiceException(OperationCodes.UserNotFound,
                     $"User with id: {userId} has not been found.");
            }

            await _remarkRepository.UpdateUserNamesAsync(userId, name);
        }

        public async Task DeleteAsync(Guid id)
        {
            Logger.Debug($"Deleting remark with id: '{id}'.");
            var remark = await _remarkRepository.GetByIdAsync(id);
            if (remark.HasNoValue)
            {
                throw new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: '{id}' does not exist!");
            }
            await _remarkPhotoService.RemovePhotosAsync(remark.Value.Id);
            await _remarkRepository.DeleteAsync(remark.Value);
            Logger.Debug($"Remark with id: '{id}' was deleted.");
        }

        public async Task SubmitVoteAsync(Guid remarkId, string userId, bool positive, DateTime createdAt)
        {
            var remark = await _remarkRepository.GetByIdAsync(remarkId);
            if (remark.HasNoValue)
            {
                throw new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: '{remarkId}' does not exist!");
            }
            if (positive)
            {
                remark.Value.VotePositive(userId, createdAt);
            } 
            else
            {
                remark.Value.VoteNegative(userId, createdAt);
            }
            await _remarkRepository.UpdateAsync(remark.Value);
        }

        public async Task DeleteVoteAsync(Guid remarkId, string userId)
        {
            var remark = await _remarkRepository.GetByIdAsync(remarkId);
            if (remark.HasNoValue)
            {
                throw new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: '{remarkId}' does not exist!");
            }

            remark.Value.DeleteVote(userId);
            await _remarkRepository.UpdateAsync(remark.Value);
        }
    }
}