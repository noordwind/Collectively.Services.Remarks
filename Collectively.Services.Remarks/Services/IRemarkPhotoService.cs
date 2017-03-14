using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    public interface IRemarkPhotoService
    {
        Task<Maybe<FileStreamInfo>> GetPhotoAsync(Guid id, string size);
        Task AddPhotosAsync(Guid id, params File[] photos);
        Task UploadImagesWithDifferentSizesAsync(Remark remark, File originalPhoto, string metadata = null);
        Task<Maybe<IEnumerable<string>>> GetPhotosForGroupsAsync(Guid id, params Guid[] groupIds);
        Task RemovePhotosAsync(Guid id, params string[] names);
    }
}