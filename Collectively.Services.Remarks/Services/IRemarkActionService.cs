using System;
using System.Threading.Tasks;

namespace Collectively.Services.Remarks.Services
{
    public interface IRemarkActionService
    {
         Task ParticipateAsync(Guid remarkId, string userId, string description);
         Task CancelParticipationAsync(Guid remarkId, string userId);
    }
}