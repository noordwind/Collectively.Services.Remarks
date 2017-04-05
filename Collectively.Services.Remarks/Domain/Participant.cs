using System;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class Participant : ValueObject<Participant>
    {
        public RemarkUser User { get; protected set; }
        public string Description { get; protected set; }
        public DateTime CreatedAt { get; protected set; }

        protected Participant()
        {
        }

        protected Participant(User user, string description)
        {
            if (user == null)
            {
                throw new ArgumentException("User can not be null.", nameof(user));
            }
            if (description?.Length > 2000)
            {
                throw new ArgumentException("Description can not have more than 2000 characters.", 
                                            nameof(description));
            }
            User = RemarkUser.Create(user);
            Description = description.Trim();
            CreatedAt = DateTime.UtcNow;
        }

        public static Participant Create(User user, string description)
            => new Participant(user, description);

        protected override bool EqualsCore(Participant other) 
            => User.Equals(other.User) 
                && CreatedAt.Equals(other.CreatedAt)
                && Description == other.Description;

        protected override int GetHashCodeCore() 
            => User.GetHashCode();
    }
}