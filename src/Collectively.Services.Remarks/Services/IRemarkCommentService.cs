using System;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    public interface IRemarkCommentService
    {
         Task ValidateEditorAccessOrFailAsync(Guid remarkId, Guid commentId, string userId);
         Task<Maybe<Comment>> GetAsync(Guid remarkId, Guid commentId);
         Task AddAsync(Guid remarkId, Guid commentId, string userId, string text);
         Task EditAsync(Guid remarkId, Guid commentId, string text);
         Task RemoveAsync(Guid remarkId, Guid commentId);
         Task SubmitVoteAsync(Guid remarkId, Guid commentId, string userId, bool positive, DateTime createdAt);
         Task DeleteVoteAsync(Guid remarkId, Guid commentId, string userId);
    }
}