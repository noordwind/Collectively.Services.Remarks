using System;
using Coolector.Common.Events;

namespace Coolector.Services.Remarks.Shared.Events
{
    public class RemarkDeleted : IAuthenticatedEvent
    {
        public Guid RequestId { get; }
        public Guid Id { get; }
        public string UserId { get; }

        protected RemarkDeleted()
        {
        }

        public RemarkDeleted(Guid requestId, Guid id, string userId)
        {
            RequestId = requestId;
            Id = id;
            UserId = userId;
        }
    }
}