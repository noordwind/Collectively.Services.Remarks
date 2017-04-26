using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collectively.Common.Files;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    public interface IRemarkPhotoService
    {
        Task AddPhotosAsync(Guid remarkId, string userId, params File[] photos);
        Task UploadImagesWithDifferentSizesAsync(Remark remark, string userId, File originalPhoto, string metadata = null);
        Task<Maybe<IEnumerable<string>>> GetPhotosForGroupsAsync(Guid remarkId, params Guid[] groupIds);
        Task RemovePhotosAsync(Guid remarkId, params string[] names);
    }
}