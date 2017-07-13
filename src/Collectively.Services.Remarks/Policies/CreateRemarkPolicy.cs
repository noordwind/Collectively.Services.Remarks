using System.Threading.Tasks;
using Collectively.Services.Remarks.Repositories;

namespace Collectively.Services.Remarks.Policies
{
    public class CreateRemarkPolicy : PolicyBase, ICreateRemarkPolicy
    {
        private readonly IRemarkRepository _remarkRepository;

        public CreateRemarkPolicy(IRemarkRepository remarkRepository)
        {
            _remarkRepository = remarkRepository;
        }

        public async Task ValidateAsync(string userId)
        {
            var latestRemark = await _remarkRepository.GetLatestUserRemarkAsync(userId);
            if(latestRemark.HasNoValue)
            {
                return;
            }
            Validate(latestRemark.Value, OperationCodes.CannotCreateRemarkTooOften,
                $"Can not create a remark too often. User: '{userId}'.");
        }
    }
}