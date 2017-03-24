using System;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    //TODO: Implement methods.
    public interface IRemarkCommentService
    {
         Task DoSomethingAsync();
         Task ValidateEditorAccessOrFailAsync(Guid remarkId, Guid commentId, string userId);
         Task<Maybe<Comment>> GetAsync(Guid remarkId, Guid commentId);
         Task AddAsync(Guid remarkId, Guid commentId, string userId, string text);
         Task EditAsync(Guid remarkId, Guid commentId, string text);
         Task RemoveAsync(Guid remarkId, Guid commentId);
    }
}