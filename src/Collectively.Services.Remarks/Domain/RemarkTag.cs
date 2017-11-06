using System;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Domain
{
    public class RemarkTag : ValueObject<RemarkTag>
    {
        public Guid Id { get; protected set; }
        public string Name { get; protected set; }
        public string Default { get; protected set; }
        public Guid DefaultId { get; protected set; }

        protected RemarkTag() 
        {
        }

        protected RemarkTag(Guid id, string name, Guid defaultId, string @default)
        {
            if (name.Empty())
            {
                throw new ArgumentException("Tag name can not be empty.", nameof(name));
            }
            if (@default.Empty())
            {
                throw new ArgumentException("Tag default name can not be empty.", nameof(name));
            }
            Id = id;
            Name = name;
            DefaultId = defaultId;
            Default = @default;
        }

        public static RemarkTag Create(Guid id, string name, Guid defaultId, string @default)
            => new RemarkTag(id, name, defaultId, @default);

        protected override bool EqualsCore(RemarkTag other) 
            => Id.Equals(other.Id);

        protected override int GetHashCodeCore() 
            => Id.GetHashCode();
    }
}