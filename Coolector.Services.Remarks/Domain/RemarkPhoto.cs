using System;
using Coolector.Common.Extensions;
using Coolector.Common.Domain;

namespace Coolector.Services.Remarks.Domain
{
    public class RemarkPhoto : ValueObject<RemarkPhoto>
    {
        public string Name { get; protected set; }
        public string Size { get; protected set; }
        public string Url { get; protected set; }
        public string Metadata { get; protected set; }

        protected RemarkPhoto()
        {
        }

        protected RemarkPhoto(string name, string size, string url, string metadata)
        {
            if (name.Empty())
                throw new ArgumentException("Photo name can not be empty.", nameof(size));
            if (size.Empty())
                throw new ArgumentException("Photo size can not be empty.", nameof(size));
            if (url.Empty())
                throw new ArgumentException("Photo Url can not be empty.", nameof(url));

            Name = name;
            Size = size;
            Url = url;
            Metadata = metadata;
        }

        public static RemarkPhoto Empty => new RemarkPhoto();

        public static RemarkPhoto Small(string name, string url, string metadata = null)
            => new RemarkPhoto(name, "small", url, metadata);

        public static RemarkPhoto Medium(string name, string url, string metadata = null)
            => new RemarkPhoto(name, "medium", url, metadata);

        public static RemarkPhoto Big(string name, string url, string metadata = null)
            => new RemarkPhoto(name, "big", url, metadata);

        public static RemarkPhoto Original(string name, string url, string metadata = null)
            => new RemarkPhoto(name, "original", url, metadata);

        public static RemarkPhoto Create(string name, string size, string url, string metadata = null)
            => new RemarkPhoto(name, size, url, metadata);

        protected override bool EqualsCore(RemarkPhoto other) => Url.Equals(other.Url);

        protected override int GetHashCodeCore() => Url.GetHashCode();
    }
}