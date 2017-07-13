using System;
using  Collectively.Common.Domain;
using  Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Domain
{
    public class Tag : IdentifiableEntity
    {
        public string Name { get; protected set; }

        protected Tag()
        {
        }

        public Tag(string name)
        {
            if (name.Empty())
            {
                throw new ArgumentException("Tag name can not be empty.", nameof(name));
            }

            Name = name.ToLowerInvariant();
        }
    }
}