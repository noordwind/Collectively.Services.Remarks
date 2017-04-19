using System;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Services.Remarks.Extensions;
using Collectively.Services.Remarks.Repositories;

namespace Collectively.Services.Remarks.Policies
{
    public class AddCommentPolicy : PolicyBase, IAddCommentPolicy
    {
        private readonly IRemarkRepository _remarkRepository;

        public AddCommentPolicy(IRemarkRepository remarkRepository)
        {
            _remarkRepository = remarkRepository;
        }

        public async Task ValidateAsync(Guid remarkId, string userId)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var latestComment = remark.Comments.OrderBy(x => x.CreatedAt)
                .LastOrDefault(x => x.User.UserId == userId);
            Validate(latestComment, OperationCodes.CannotAddCommentTooOften,
                $"Can not add remark comment too often. Remark: '{remarkId}', user: '{userId}'.");
        }
    }
}