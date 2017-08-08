using System;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Domain
{
    public class RemarkGroup : ValueObject<RemarkGroup>
    {
        public Guid Id { get; protected set; }
        public string Name { get; protected set; }

        protected RemarkGroup() 
        {
        }

        protected RemarkGroup(Guid id, string name)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Group id can not be empty.", nameof(name));
            }
            if (name.Empty())
            {
                throw new ArgumentException("Group name can not be empty.", nameof(name));
            }
            Id = id;
            Name = name;
        }

        public static RemarkGroup Create(Group group)
            => new RemarkGroup(group.Id, group.Name);

        protected override bool EqualsCore(RemarkGroup other) 
            => Id.Equals(other.Id);

        protected override int GetHashCodeCore() 
            => Id.GetHashCode();
    }
}