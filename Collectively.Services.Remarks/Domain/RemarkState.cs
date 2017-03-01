using System;
using  Collectively.Common.Domain;
using  Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Domain
{
    public class RemarkState : ValueObject<RemarkState>
    {
        public string State { get; protected set; }
        public RemarkUser User { get; protected set; }
        public string Description { get; protected set; }
        public Location Location { get; protected set; }
        public DateTime CreatedAt { get; protected set; }

        public static class Names
        {
            public static string New => "new";
            public static string Processing => "processing";
            public static string Resolved => "resolved";
            public static string Renewed => "renewed";
            public static string Canceled => "canceled";
        }

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
            Location = location;
            CreatedAt = DateTime.UtcNow;
        }

        public static RemarkState New(RemarkUser user, Location location, string description = null) 
            => new RemarkState(Names.New, user, description, location);

        public static RemarkState Processing(RemarkUser user, string description = null) 
            => new RemarkState(Names.Processing, user, description);

        public static RemarkState Resolved(RemarkUser user, Location location, string description = null) 
            => new RemarkState(Names.Resolved, user, description, location);

        public static RemarkState Renewed(RemarkUser user, string description = null) 
            => new RemarkState(Names.Renewed, user, description);

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