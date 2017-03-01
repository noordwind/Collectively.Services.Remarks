using System;
using  Collectively.Common.Extensions;
using  Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class RemarkUser : ValueObject<RemarkUser>
    {
        public string UserId { get; protected set; }
        public string Name { get; protected set; }

        protected RemarkUser() 
        {
        }

        protected RemarkUser(string userId, string name)
        {
            if (userId.Empty())
            {
                throw new ArgumentException("User id can not be empty.", nameof(name));
            }
            if (name.Empty())
            {
                throw new ArgumentException("User name can not be empty.", nameof(name));
            }

            UserId = userId;
            Name = name;
        }

        public static RemarkUser Create(User user)
            => new RemarkUser(user.UserId, user.Name);

        protected override bool EqualsCore(RemarkUser other) 
            => UserId.Equals(other.UserId) && Name.Equals(other.Name);

        protected override int GetHashCodeCore() 
            => UserId.GetHashCode() ^ Name.GetHashCode();
    }
}