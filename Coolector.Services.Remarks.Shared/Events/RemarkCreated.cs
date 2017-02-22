using System;
using System.Collections.Generic;
using Coolector.Common.Events;
using Coolector.Services.Remarks.Shared.Events.Models;

namespace Coolector.Services.Remarks.Shared.Events
{
    public class RemarkCreated : IAuthenticatedEvent
    {
        public Guid RequestId { get; }
        public Guid RemarkId { get; }
        public string UserId { get; }
        public string Username { get; }
        public RemarkCategory Category { get; }
        public RemarkLocation Location { get; }
        public string Description { get; }
        public RemarkState State { get; }
        public IEnumerable<string> Tags { get; set; }
        public DateTime CreatedAt { get; }

        protected RemarkCreated()
        {
        }

        public RemarkCreated(Guid requestId, Guid remarkId, 
            string userId, string username,
            RemarkCategory category, RemarkLocation location,
            string description, IEnumerable<string> tags, DateTime createdAt)
        {
            RequestId = requestId;
            RemarkId = remarkId;
            UserId = userId;
            Username = username;
            Category = category;
            Location = location;
            Description = description;
            Tags = tags;
            CreatedAt = createdAt;
            State = new RemarkState
            {
                State = "new",
                UserId = userId,
                Username = username,
                Description = description,
                Location = location,
                CreatedAt = createdAt
            };
        }
    }
}