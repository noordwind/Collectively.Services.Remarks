using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coolector.Common.Types;
using Coolector.Common.Domain;
using Coolector.Common.Extensions;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Extensions;
using Coolector.Services.Remarks.Queries;
using Coolector.Services.Remarks.Repositories;
using Coolector.Services.Remarks.Shared;
using Coolector.Services.Remarks.Settings;
using NLog;
using File = Coolector.Services.Remarks.Domain.File;
using System.Text.RegularExpressions;

namespace Coolector.Services.Remarks.Services
{
    public class RemarkService : IRemarkService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IFileHandler _fileHandler;
        private readonly IRemarkRepository _remarkRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IImageService _imageService;
        private readonly IUserRepository _userRepository;
        private readonly IUniqueNumberGenerator _uniqueNumberGenerator;
        private readonly GeneralSettings _settings;

        public RemarkService(IFileHandler fileHandler, 
            IRemarkRepository remarkRepository, 
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            IImageService imageService,
            IUniqueNumberGenerator uniqueNumberGenerator,
            GeneralSettings settings)
        {
            _fileHandler = fileHandler;
            _remarkRepository = remarkRepository;
            _categoryRepository = categoryRepository;
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

        public async Task<Maybe<FileStreamInfo>> GetPhotoAsync(Guid id, string size)
            => await _fileHandler.GetFileStreamInfoAsync(id, size);

        public async Task CreateAsync(Guid id, string userId, string category, File photo,
            Location location, string description = null)
        {
            Logger.Debug($"Create remark, id:{id}, userId:{userId}, category: {category}, " +
                         $"photo: {photo.Name}, lat: {location.Latitude}, lng: {location.Longitude}");
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
                throw new ArgumentException($"User with id: {userId} has not been found.");

            var remarkCategory = await _categoryRepository.GetByNameAsync(category);
            if (remarkCategory.HasNoValue)
                throw new ArgumentException($"Category {category} has not been found.");

            var remark = new Remark(id, user.Value, remarkCategory.Value, location, description);
            await UploadImagesWithDifferentSizesAsync(remark, photo);
            await _remarkRepository.AddAsync(remark);
        }

        public async Task ResolveAsync(Guid id, string userId, File photo = null, Location location = null)
        {
            Logger.Debug($"Resolve remark, id:{id}, userId:{userId}, photo:{photo?.Name ?? "none"}");
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
                throw new ArgumentException($"User with id: {userId} has not been found.");

            var remark = await _remarkRepository.GetByIdAsync(id);
            if (remark.HasNoValue)
            {
                throw new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: {id} does not exist!");
            }

            if (location != null && remark.Value.Location.IsInRange(location, _settings.AllowedDistance) == false)
            {
                throw new ServiceException(OperationCodes.DistanceBetweenUserAndRemarkIsTooBig,
                    $"The distance between user and remark: {id} is too big! " +
                    $"lat:{location.Latitude}, long:{location.Longitude}");
            }

            if (photo != null)
                await UploadImagesWithDifferentSizesAsync(remark.Value, photo, "resolved");

            remark.Value.Resolve(user.Value);
            await _remarkRepository.UpdateAsync(remark.Value);
        }

        public async Task UpdateUserNamesAsync(string userId, string name)
        {
            Logger.Debug($"Update user's remarks with new userName, userid: {userId}, userName: {name}");
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
            {
                throw new ArgumentException($"User with id: {userId} has not been found.");
            }

            await _remarkRepository.UpdateUserNamesAsync(userId, name);
        }

        public async Task DeleteAsync(Guid id, string userId)
        {
            Logger.Debug($"Delete remark, id:{id}, userId: {userId}");
            var remark = await _remarkRepository.GetByIdAsync(id);
            if (remark.HasNoValue)
            {
                throw new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: {id} does not exist!");
            }
            if (remark.Value.Author.UserId != userId)
            {
                throw new ServiceException(OperationCodes.UserNotAllowedToDeleteRemark,
                    $"User: {userId} is not allowed to delete remark: {id}");
            }

            await _remarkRepository.DeleteAsync(remark.Value);
            foreach (var photo in remark.Value.Photos)
            {
                if (photo.Name.Empty())
                    continue;

                await _fileHandler.DeleteAsync(photo.Name);
            }
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
            if(remark.Value.Photos.Count() + photos.Count() > _settings.PhotosLimit) 
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
            var tasks = new List<Task>();
            foreach (var photo in photos)
            {
                var size = photo.Key;
                var fileName = metadata == null 
                    ? $"{remark.Id:N}_{size}_{uniqueNumber}.{extension}"
                    : $"{remark.Id:N}_{size}_{metadata}_{uniqueNumber}.{extension}";
                var task = _fileHandler.UploadAsync(photo.Value, fileName, url =>
                {
                    remark.AddPhoto(RemarkPhoto.Create(fileName, size, url, metadata));
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }
    }
}