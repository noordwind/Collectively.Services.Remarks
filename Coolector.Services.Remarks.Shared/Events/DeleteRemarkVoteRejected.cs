using System;
using Coolector.Common.Events;

namespace Coolector.Services.Remarks.Shared.Events
{
    public class DeleteRemarkVoteRejected : IRejectedEvent
    {
        public Guid RequestId { get; }
        public Guid RemarkId { get; }
        public string UserId { get; }
        public string Code { get; }
        public string Reason { get; }

        protected DeleteRemarkVoteRejected()
        {
        }

        public DeleteRemarkVoteRejected(Guid requestId, 
            string userId, Guid remarkId,
            string code, string reason)
        {
            RequestId = requestId;
            UserId = userId;
            RemarkId = remarkId;
            Code = code;
            Reason = reason;
        }
    }
}