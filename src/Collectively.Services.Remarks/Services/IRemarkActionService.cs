using System;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Services
{
    public interface IRemarkActionService
    {
        Task<Maybe<Participant>> GetParticipantAsync(Guid remarkId, string userId);
        Task ParticipateAsync(Guid remarkId, string userId, string description);
        Task CancelParticipationAsync(Guid remarkId, string userId);
    }
}