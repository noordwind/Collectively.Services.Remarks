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
using NLog;
using File = Coolector.Services.Remarks.Domain.File;

namespace Coolector.Services.Remarks.Services
{
    public class RemarkService : IRemarkService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const double AllowedDistance = 15.0;

        private readonly IFileHandler _fileHandler;
        private readonly IRemarkRepository _remarkRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IImageService _imageService;
        private readonly IUserRepository _userRepository;

        public RemarkService(IFileHandler fileHandler, 
            IRemarkRepository remarkRepository, 
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            IImageService imageService)
        {
            _fileHandler = fileHandler;
            _remarkRepository = remarkRepository;
            _categoryRepository = categoryRepository;
            _imageService = imageService;
            _userRepository = userRepository;
        }

        public async Task<Maybe<Remark>> GetAsync(Guid id)
            => await _remarkRepository.GetByIdAsync(id);

        public async Task<Maybe<PagedResult<Remark>>> BrowseAsync(BrowseRemarks query)
            => await _remarkRepository.BrowseAsync(query);

        public async Task<Maybe<PagedResult<Category>>> BrowseCategoriesAsync(BrowseCategories query)
            => await _categoryRepository.BrowseAsync(query);

        public async Task<Maybe<FileStreamInfo>> GetPhotoAsync(Guid id, string size)
            => await _fileHandler.GetFileStreamInfoAsync(id, size);

        public async Task CreateAsync(Guid id, string userId, Guid categoryId, File photo, 
            Location location, string description = null)
        {
            Logger.Debug($"Create remark, id:{id}, userId:{userId}, categoryId:{categoryId}, photo:{photo.Name}, lat:{location.Latitude}, long:{location.Longitude}");
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
                throw new ArgumentException($"User with id: {userId} has not been found.");

            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category.HasNoValue)
                throw new ArgumentException($"Category with id: {userId} has not been found.");

            var remark = new Remark(id, user.Value, category.Value, location, description);
            await UploadImagesWithDifferentSizesAsync(remark, photo);
            await _remarkRepository.AddAsync(remark);
        }

        public async Task ResolveAsync(Guid id, string userId, File photo, Location location)
        {
            Logger.Debug($"Resolve remark, id:{id}, userId:{userId}, photo:{photo.Name}, lat:{location.Latitude}, long:{location.Longitude}");
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
                throw new ArgumentException($"User with id: {userId} has not been found.");

            var remark = await _remarkRepository.GetByIdAsync(id);
            if (remark.HasNoValue)
                throw new ServiceException($"Remark with id: {id} does not exist!");

            if (remark.Value.Location.IsInRange(location, AllowedDistance) == false)
                throw new ServiceException($"The distance between user and remark: {id} is to high!");

            await UploadImagesWithDifferentSizesAsync(remark.Value, photo, "resolved");
            remark.Value.Resolve(user.Value);
            await _remarkRepository.UpdateAsync(remark.Value);
        }

        public async Task UpdateUserNamesAsync(string userId, string name)
        {
            Logger.Debug($"Update user's remarks with new userName, userid: {userId}, userName: {name}");
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasNoValue)
                throw new ArgumentException($"User with id: {userId} has not been found.");

            await _remarkRepository.UpdateUserNamesAsync(userId, name);
        }

        public async Task DeleteAsync(Guid id, string userId)
        {
            Logger.Debug($"Delete remark, id:{id}, userId: {userId}");
            var remark = await _remarkRepository.GetByIdAsync(id);
            if (remark.HasNoValue)
                throw new ServiceException($"Remark with id: {id} does not exist!");

            if (remark.Value.Author.UserId != userId)
                throw new ServiceException($"User: {userId} is not allowed to delete remark: {id}");

            await _remarkRepository.DeleteAsync(remark.Value);
            foreach (var photo in remark.Value.Photos)
            {
                if (photo.Name.Empty())
                    continue;

                await _fileHandler.DeleteAsync(photo.Name);
            }
        }

        private async Task UploadImagesWithDifferentSizesAsync(Remark remark, File originalPhoto, string metadata = null)
        {
            var extension = originalPhoto.Name.Split('.').Last();
            var photos = _imageService.ProcessImage(originalPhoto);
            var tasks = new List<Task>();
            foreach (var photo in photos)
            {
                var size = photo.Key;
                var fileName = $"{remark.Id:N}_{size}_{metadata}.{extension}";
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