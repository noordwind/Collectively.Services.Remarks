using System;
using Coolector.Common.Events;
using Coolector.Services.Remarks.Shared.Events.Models;

namespace Coolector.Services.Remarks.Shared.Events
{
    public abstract class RemarkStateChangedBase : IAuthenticatedEvent
    {
        public Guid RequestId { get; }
        public Guid RemarkId { get; }
        public string UserId { get; }
        public string Username { get; }
        public string State { get; }
        public string Description { get; }
        public DateTime CreatedAt { get; }
        public RemarkLocation Location { get; }

        protected RemarkStateChangedBase()
        {
        }

        public RemarkStateChangedBase(Guid requestId, Guid remarkId, 
            string userId, string username, string state, 
            string description, RemarkLocation location,
            DateTime createdAt)
        {
            RequestId = requestId;
            RemarkId = remarkId;
            UserId = userId;
            Username = username;
            State = state;
            Description = description;
            Location = location;
            CreatedAt = createdAt;
        }
    }
}