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
using File = Collectively.Services.Remarks.Domain.File;

namespace Collectively.Services.Remarks.Services
{
    public class RemarkService : IRemarkService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IFileHandler _fileHandler;
        private readonly IRemarkRepository _remarkRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IImageService _imageService;
        private readonly IUserRepository _userRepository;
        private readonly IUniqueNumberGenerator _uniqueNumberGenerator;
        private readonly GeneralSettings _settings;

        public RemarkService(IFileHandler fileHandler, 
            IRemarkRepository remarkRepository, 
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            ITagRepository tagRepository,
            IImageService imageService,
            IUniqueNumberGenerator uniqueNumberGenerator,
            GeneralSettings settings)
        {
            _fileHandler = fileHandler;
            _remarkRepository = remarkRepository;
            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
            _imageService = imageService;
            _userRepository = userRepository;
            _uniqueNumberGenerator = uniqueNumberGenerator;
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

        public async Task<Maybe<FileStreamInfo>> GetPhotoAsync(Guid id, string size)
            => await _fileHandler.GetFileStreamInfoAsync(id, size);

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

        public async Task ResolveAsync(Guid id, string userId, File photo = null, Location location = null, bool validateLocation = false)
        {
            Logger.Debug($"Resolve remark, id:{id}, userId:{userId}, photo:{photo?.Name ?? "none"}");
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
            {
                throw new ServiceException(OperationCodes.UserNotFound,
                    $"User with id: '{userId}' does not exist!");
            }

            var remark = await _remarkRepository.GetByIdAsync(id);
            if (remark.HasNoValue)
            {
                throw new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: {id} does not exist!");
            }

            if (location != null && validateLocation && remark.Value.Location.IsInRange(location, _settings.AllowedDistance) == false)
            {
                throw new ServiceException(OperationCodes.DistanceBetweenUserAndRemarkIsTooBig,
                    $"The distance between user and remark: {id} is too big! " +
                    $"lat:{location.Latitude}, long:{location.Longitude}");
            }

            if (photo != null)
            {
                await UploadImagesWithDifferentSizesAsync(remark.Value, photo, RemarkState.Names.Resolved);
            }

            remark.Value.SetResolvedState(user.Value, location);
            await _remarkRepository.UpdateAsync(remark.Value);
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

            await _remarkRepository.DeleteAsync(remark.Value);
            foreach (var photo in remark.Value.Photos)
            {
                if (photo.Name.Empty())
                    continue;

                await _fileHandler.DeleteAsync(photo.Name);
            }
            Logger.Debug($"Remark with id: '{id}' was deleted.");
        }

        public async Task AddPhotosAsync(Guid id, params File[] photos)
        {
            if (photos == null || !photos.Any())
            {
                throw new ServiceException(OperationCodes.NoFiles, 
                    $"There are no photos to be added to the remark with id: '{id}'.");
            }

            Logger.Debug($"Adding {photos.Count()} photos to remark with id: '{id}'.");
            var remark = await _remarkRepository.GetByIdAsync(id);
            if (remark.HasNoValue)
            {
                throw new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: {id} does not exist!");
            }
            if(remark.Value.Photos.GroupBy(x => x.Size).Count() + photos.Count() > _settings.PhotosLimit) 
            {
                throw new ServiceException(OperationCodes.TooManyFiles,
                    $"There are too many photos ({photos.Count()}) to be added to the remark with id: '{id}'.");
            }

            var tasks = new List<Task>();
            foreach(var photo in photos)
            {
                var task = UploadImagesWithDifferentSizesAsync(remark.Value, photo);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            await _remarkRepository.UpdateAsync(remark.Value);
            Logger.Debug($"Added {photos.Count()} photos to remark with id: '{id}'.");
        }

        private async Task UploadImagesWithDifferentSizesAsync(Remark remark, File originalPhoto, string metadata = null)
        {
            var extension = originalPhoto.Name.Split('.').Last();
            var photos = _imageService.ProcessImage(originalPhoto);
            var uniqueNumber = _uniqueNumberGenerator.Generate();
            var groupId = Guid.NewGuid();
            var tasks = new List<Task>();
            foreach (var photo in photos)
            {
                var size = photo.Key;
                var fileName = metadata == null 
                    ? $"{remark.Id:N}_{size}_{uniqueNumber}.{extension}"
                    : $"{remark.Id:N}_{size}_{metadata}_{uniqueNumber}.{extension}";
                var task = _fileHandler.UploadAsync(photo.Value, fileName, url =>
                {
                    remark.AddPhoto(RemarkPhoto.Create(groupId, fileName, size, url, metadata));
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }


        public async Task<Maybe<IEnumerable<string>>> GetPhotosForGroupsAsync(Guid id, params Guid[] groupIds)
        {
            if (groupIds == null || !groupIds.Any())
            {
                return null;
            }

            var remark = await _remarkRepository.GetByIdAsync(id);
            if (remark.HasNoValue)
            {
                return null;
            }

            return remark.Value.Photos
                            .Where(x => groupIds.Contains(x.GroupId))
                            .Select(x => x.Name)
                            .ToList();
        }

        public async Task RemovePhotosAsync(Guid id, params string[] names)
        {
            if (names == null || !names.Any())
            {
                throw new ServiceException(OperationCodes.NoFiles, 
                    $"There are no photos to be removed from the remark with id: '{id}'.");
            }

            var remark = await _remarkRepository.GetByIdAsync(id);
            if (remark.HasNoValue)
            {
                throw new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: {id} does not exist!");
            }

            Logger.Debug($"Removing {names.Count()} photos from the remark with id: '{id}'.");
            foreach (var name in names)
            {
                if(remark.Value.GetPhoto(name).HasNoValue)
                {
                    throw new ServiceException(OperationCodes.FileNotFound,
                        $"Remark photo with name: '{name}' was not found in the remark with id: '{id}'.");
                }
                remark.Value.RemovePhoto(name);
            }
            await _remarkRepository.UpdateAsync(remark.Value);

            var tasks = new List<Task>();
            foreach (var photo in names)
            {
                var task = _fileHandler.DeleteAsync(photo);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            Logger.Debug($"Removed {names.Count()} photos from the remark with id: '{id}'.");
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