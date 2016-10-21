using System;
using Coolector.Common.Extensions;
using Coolector.Common.Domain;

namespace Coolector.Services.Remarks.Domain
{
    public class RemarkPhoto : ValueObject<RemarkPhoto>
    {
        public string Id { get; protected set; }
        public string Size { get; protected set; }
        public string Url { get; protected set; }
        public string Metadata { get; protected set; }

        protected RemarkPhoto()
        {
        }

        protected RemarkPhoto(string size, string url, string metadata, string id = null)
        {
            if (size.Empty())
                throw new ArgumentException("Photo size can not be empty.", nameof(size));
            if (url.Empty())
                throw new ArgumentException("Photo Url can not be empty.", nameof(url));

            Size = size;
            Url = url;
            Metadata = metadata;
            Id = id;
        }

        public static RemarkPhoto Empty => new RemarkPhoto();

        public static RemarkPhoto Small(string url, string metadata = null, string id = null)
            => new RemarkPhoto("small", url, metadata, id);

        public static RemarkPhoto Medium(string url, string metadata = null, string id = null)
            => new RemarkPhoto("medium", url, metadata, id);

        public static RemarkPhoto Big(string url, string metadata = null, string id = null)
            => new RemarkPhoto("big", url, metadata, id);

        public static RemarkPhoto Original(string url, string metadata = null, string id = null)
            => new RemarkPhoto("original", url, metadata, id);

        protected override bool EqualsCore(RemarkPhoto other) => Url.Equals(other.Url);

        protected override int GetHashCodeCore() => Url.GetHashCode();
    }
}