using System;
using Coolector.Common.Domain;
using Coolector.Common.Extensions;

namespace Coolector.Services.Remarks.Domain
{
    public class RemarkState : ValueObject<RemarkState>
    {
        public string State { get; protected set; }
        public RemarkUser User { get; protected set; }
        public string Description { get; protected set; }
        public Location Location { get; protected set; }
        public DateTime CreatedAt { get; protected set; }

        protected RemarkState()
        {
        }

        protected RemarkState(string state, RemarkUser user, string description = null, Location location = null)
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

            State = state;
            User = user;
            Description = description?.Trim() ?? string.Empty;
            CreatedAt = DateTime.UtcNow;
        }

        public static RemarkState New(RemarkUser user, string description = null) 
            => new RemarkState("new", user, description);

        public static RemarkState Processing(RemarkUser user, string description = null) 
            => new RemarkState("processing", user, description);

        public static RemarkState Resolved(RemarkUser user, Location location, string description = null) 
            => new RemarkState("resolved", user, description, location);

        public static RemarkState Renewed(RemarkUser user, string description = null) 
            => new RemarkState("renewed", user, description);

        public static RemarkState Canceled(RemarkUser user, string description = null) 
            => new RemarkState("canceled", user, description);

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