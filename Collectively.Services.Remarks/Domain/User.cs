using System;
using  Collectively.Common.Domain;
using  Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Domain
{
    public class User : IdentifiableEntity, ITimestampable
    {
        public string UserId { get; protected set; }
        public string Name { get; protected set; }
        public string Role { get; protected set; }
        public DateTime CreatedAt { get; protected set; }

        protected User()
        {
        }

        public User(string userId, string name, string role)
        {
            UserId = userId;
            Name = name;
            Role = role;
            CreatedAt = DateTime.UtcNow;
        }

        public void SetName(string name)
        {
            if (name.Empty())
                throw new ArgumentException("User name can not be empty.", nameof(name));
            if (name.Length > 50)
                throw new ArgumentException("User name is too long.", nameof(name));
            if (Name.EqualsCaseInvariant(name))
                return;

            Name = name.ToLowerInvariant();
        }
    }
}