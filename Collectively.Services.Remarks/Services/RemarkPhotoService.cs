using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Common.Domain;
using Collectively.Common.Files;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories;
using Collectively.Services.Remarks.Settings;
using NLog;

namespace Collectively.Services.Remarks.Services
{
    public class RemarkPhotoService : IRemarkPhotoService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IRemarkRepository _remarkRepository;
        private readonly IFileHandler _fileHandler;
        private readonly IImageService _imageService;
        private readonly IUniqueNumberGenerator _uniqueNumberGenerator;
        private readonly GeneralSettings _settings;

        public RemarkPhotoService(IRemarkRepository remarkRepository, 
            IFileHandler fileHandler, IImageService imageService,
            IUniqueNumberGenerator uniqueNumberGenerator,
            GeneralSettings settings)
        {
            _remarkRepository = remarkRepository;
            _fileHandler = fileHandler;
            _imageService = imageService;
            _uniqueNumberGenerator = uniqueNumberGenerator;
            _settings = settings;
        }

        public async Task<Maybe<FileStreamInfo>> GetPhotoAsync(Guid id, string size)
            => await _fileHandler.GetFileStreamInfoAsync(id, size);

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

        public async Task UploadImagesWithDifferentSizesAsync(Remark remark, File originalPhoto, string metadata = null)
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
    }
}