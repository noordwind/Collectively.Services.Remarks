using System;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class OfferingProposal : IdentifiableEntity, ITimestampable
    {
        public Guid RemarkId { get; protected set; }
        public string UserId { get; protected set; }
        public string Status { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime UpdatedAt { get; protected set; }

        protected OfferingProposal()
        {
        }   

        public OfferingProposal(Guid remarkId, string userId)
        {
            RemarkId = remarkId;
            UserId = userId;
            Status = State.New;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Accept()
        {
            ValidateIfCanUpdateOrFail();
            Status = State.Accepted;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Refuse()
        {
            ValidateIfCanUpdateOrFail();
            Status = State.Refused;
            UpdatedAt = DateTime.UtcNow;
        }

        private void ValidateIfCanUpdateOrFail()
        {
            if (Status == State.New)
            {
                return;
            }
            throw new ServiceException(OperationCodes.OfferingProposalAlreadyUpdated,
                $"Offering proposal for remark: '{RemarkId}' and user: {UserId} " +
                $"was already updated at '{UpdatedAt}' with status: '{Status}'.");
        }

        public static class State
        {
            public static string New => "new";
            public static string Accepted => "accepted";
            public static string Refused => "refused";
        }   
    }
}