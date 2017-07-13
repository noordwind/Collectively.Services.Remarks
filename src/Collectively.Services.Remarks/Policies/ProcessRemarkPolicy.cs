using System;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Services.Remarks.Extensions;
using Collectively.Services.Remarks.Repositories;

namespace Collectively.Services.Remarks.Policies
{
    public class ProcessRemarkPolicy : PolicyBase, IProcessRemarkPolicy
    {
        private readonly IRemarkRepository _remarkRepository;

        public ProcessRemarkPolicy(IRemarkRepository remarkRepository)
        {
            _remarkRepository = remarkRepository;
        }

        public async Task ValidateAsync(Guid remarkId, string userId)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var latestState = remark.States.OrderBy(x => x.CreatedAt)
                .LastOrDefault(x => x.User.UserId == userId);
            Validate(latestState, OperationCodes.CannotSetStateTooOften,
                $"Can not process remark too often. Remark: '{remarkId}', user: '{userId}'.");
        }
    }
}