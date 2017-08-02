using System;
using System.Collections.Generic;
using  Collectively.Common.Domain;
using  Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Domain
{
    public class User : IdentifiableEntity, ITimestampable
    {
        private ISet<Guid> _favoriteRemarks = new HashSet<Guid>();
        public string UserId { get; protected set; }
        public string Name { get; protected set; }
        public string State { get; protected set; }
        public string Role { get; protected set; }
        public string AvatarUrl { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public IEnumerable<Guid> FavoriteRemarks
        {
            get { return _favoriteRemarks; }
            protected set { _favoriteRemarks = new HashSet<Guid>(value); }
        }

        protected User()
        {
        }

        public User(string userId, string name, string role, string state)
        {
            UserId = userId;
            Name = name;
            Role = role;
            State = state;
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

        public void SetAvatar(string avatarUrl)
        {
            AvatarUrl = avatarUrl;
        }

        public void AddFavoriteRemark(Remark remark)
        {
            _favoriteRemarks.Add(remark.Id);
        }

        public void RemoveFavoriteRemark(Remark remark)
        {
            _favoriteRemarks.Remove(remark.Id);
        }

        public void MarkAsDeleted()
        {
            State = "deleted";
        }
    }
}