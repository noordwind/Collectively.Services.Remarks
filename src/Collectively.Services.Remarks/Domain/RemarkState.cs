using System;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Domain
{
    public class RemarkState : ValueObject<RemarkState>, ITimestampable
    {
        public Guid Id { get; protected set; }
        public string State { get; protected set; }
        public string Assignee { get; protected set; }
        public RemarkUser User { get; protected set; }
        public string Description { get; protected set; }
        public Location Location { get; protected set; }
        public RemarkPhoto Photo { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public bool Removed => RemovedAt.HasValue;
        public DateTime? RemovedAt { get; protected set; }

        public static class Names
        {
            public static string New => "new";
            public static string Assigned => "assigned";
            public static string Unassigned => "unassigned";
            public static string Processing => "processing";
            public static string Resolved => "resolved";
            public static string Renewed => "renewed";
            public static string Canceled => "canceled";
        }

        protected RemarkState()
        {
        }

        protected RemarkState(string state, RemarkUser user, 
            string description = null, Location location = null, 
            RemarkPhoto photo = null, DateTime? createdAt = null,
            string assignee = null)
        {
            if (state.Empty())
            {
                throw new ArgumentException("State can not be empty.", nameof(state));
            }
            if (user == null)
            {
                throw new ArgumentException("User can not be null.", nameof(user));
            }
            if (description?.Length > 2000)
            {
                throw new ArgumentException("Description can not have more than 2000 characters.", 
                                            nameof(description));
            }
            Id = Guid.NewGuid();
            State = state;
            User = user;
            Description = description?.Trim() ?? string.Empty;
            Location = location;
            Photo = photo;
            CreatedAt = createdAt.HasValue ? createdAt.Value : DateTime.UtcNow;
            Assignee = assignee?.ToLowerInvariant();
        }

        public void Remove()
        {
            if (Removed)
            {
                throw new DomainException(OperationCodes.StateRemoved,
                    $"State: '{Id}' was removed at {RemovedAt}.");
            }
            if (State != Names.Processing)
            {
                throw new DomainException(OperationCodes.CannotRemoveNonProcessingState, 
                    "Cannot remove state different than processing.");
            }
            RemovedAt = DateTime.UtcNow;
        }

        public static RemarkState New(RemarkUser user, Location location, 
            string description = null, DateTime? createdAt = null) 
            => new RemarkState(Names.New, user, description, location, createdAt: createdAt);

        public static RemarkState AssignedToGroup(RemarkUser user, Guid groupId,
            string description = null) 
            => new RemarkState(Names.Assigned, user, description, assignee: $"group:{groupId}");

        public static RemarkState AssignedToUser(RemarkUser user, string userId,
            string description = null) 
            => new RemarkState(Names.Assigned, user, description, assignee: $"user:{userId}");

        public static RemarkState Unassigned(RemarkUser user,
            string description = null) 
            => new RemarkState(Names.Unassigned, user, description);

        public static RemarkState Processing(RemarkUser user, 
            string description = null, RemarkPhoto photo = null) 
            => new RemarkState(Names.Processing, user, description, photo: photo);

        public static RemarkState Resolved(RemarkUser user, 
            string description = null, Location location = null, RemarkPhoto photo = null) 
            => new RemarkState(Names.Resolved, user, description, location, photo);

        public static RemarkState Renewed(RemarkUser user, 
            string description = null, RemarkPhoto photo = null) 
            => new RemarkState(Names.Renewed, user, description, photo: photo);

        public static RemarkState Canceled(RemarkUser user, string description = null) 
            => new RemarkState(Names.Canceled, user, description);

        protected override bool EqualsCore(RemarkState other) 
            => State.Equals(other.State) 
                && User.Equals(other.User) 
                && CreatedAt.Equals(other.CreatedAt)
                && Description == other.Description;

        protected override int GetHashCodeCore() 
            => State.GetHashCode() ^ User.GetHashCode() 
                ^ CreatedAt.GetHashCode() ^ Description.GetHashCode();
    }
}