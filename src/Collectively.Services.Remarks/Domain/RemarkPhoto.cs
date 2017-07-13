using System;
using Collectively.Common.Extensions;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class RemarkPhoto : ValueObject<RemarkPhoto>
    {
        public Guid GroupId { get; protected set; }
        public string Name { get; protected set; }
        public string Size { get; protected set; }
        public string Url { get; protected set; }
        public string Metadata { get; protected set; }
        public RemarkUser User { get; protected set; }
        public DateTime CreatedAt { get; protected set; }

        protected RemarkPhoto()
        {
        }

        protected RemarkPhoto(Guid groupId, string name, string size, string url, RemarkUser user, string metadata)
        {
            if (groupId == Guid.Empty)
            {
                throw new ArgumentException("Photo id can not be empty.", nameof(groupId));
            }
            if (name.Empty())
            {
                throw new ArgumentException("Photo name can not be empty.", nameof(size));
            }
            if (size.Empty())
            {
                throw new ArgumentException("Photo size can not be empty.", nameof(size));
            }
            if (url.Empty())
            {
                throw new ArgumentException("Photo Url can not be empty.", nameof(url));
            }
            if (user == null)
            {
                throw new ArgumentException("Photo user can not be empty.", nameof(user));
            }
            GroupId = groupId;
            Name = name;
            Size = size;
            Url = url;
            Metadata = metadata;
            User = user;
            CreatedAt = DateTime.UtcNow;
        }

        public static RemarkPhoto Empty => new RemarkPhoto();

        public static RemarkPhoto Small(Guid groupId, string name, string url, 
            RemarkUser user, string metadata = null)
            => new RemarkPhoto(groupId, name, "small", url, user, metadata);

        public static RemarkPhoto Medium(Guid groupId, string name, string url, 
            RemarkUser user, string metadata = null)
            => new RemarkPhoto(groupId, name, "medium", url, user, metadata);

        public static RemarkPhoto Big(Guid groupId, string name, string url, 
            RemarkUser user, string metadata = null)
            => new RemarkPhoto(groupId, name, "big", url, user, metadata);

        public static RemarkPhoto Create(Guid groupId, string name, string size, string url, 
            RemarkUser user, string metadata = null)
            => new RemarkPhoto(groupId, name, size, url, user, metadata);

        protected override bool EqualsCore(RemarkPhoto other) => Name.Equals(other.Name);

        protected override int GetHashCodeCore() => Name.GetHashCode();
    }
}