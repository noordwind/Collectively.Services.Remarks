using System;
using System.Collections.Generic;
using Coolector.Common.Events;
using Coolector.Services.Remarks.Shared.Events.Models;

namespace Coolector.Services.Remarks.Shared.Events
{
    public class RemarkResolved : IAuthenticatedEvent
    {
        public Guid RequestId { get; }
        public Guid RemarkId { get; }
        public string UserId { get; }
        public string Username { get; }
        public IEnumerable<RemarkFile> Photos { get; }
        public DateTime ResolvedAt { get; }
        public RemarkLocation ResolvedAtLocation { get; }

        protected RemarkResolved()
        {
        }

        public RemarkResolved(Guid requestId, Guid remarkId, 
            string userId, string username, RemarkLocation location,
            IEnumerable<RemarkFile> photos, DateTime resolvedAt)
        {
            RequestId = requestId;
            RemarkId = remarkId;
            UserId = userId;
            Username = username;
            ResolvedAtLocation = location;
            Photos = photos;
            ResolvedAt = resolvedAt;
        }
    }
}