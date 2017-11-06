using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    public interface ITagManager
    {
        Task<Maybe<IEnumerable<RemarkTag>>> FindAsync(IEnumerable<Guid> tagsIds);
    }
}