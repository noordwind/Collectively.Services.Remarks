using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collectively.Services.Remarks.Dto;

namespace Collectively.Services.Remarks.Services
{
    public interface ITagService
    {
        Task AddOrUpdateAsync(IEnumerable<TagDto> tags);
    }
}