using System;
using System.Threading.Tasks;
using Collectively.Services.Remarks.Extensions;
using Collectively.Services.Remarks.Repositories;
using NLog;

namespace Collectively.Services.Remarks.Services
{
    public class RemarkActionService : IRemarkActionService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IRemarkRepository _remarkRepository;
        private readonly IUserRepository _userRepository;

        public RemarkActionService(IRemarkRepository remarkRepository, 
            IUserRepository userRepository)
        {
            _remarkRepository = remarkRepository;
            _userRepository = userRepository;
        }

        public async Task ParticipateAsync(Guid remarkId, string userId, string description)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var user = await _userRepository.GetOrFailAsync(userId);
            remark.Participate(user, description);
            await _remarkRepository.UpdateAsync(remark);
        }

        public async Task CancelParticipationAsync(Guid remarkId, string userId)
        {
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            remark.CancelParticipation(userId);
            await _remarkRepository.UpdateAsync(remark);
        }
    }
}