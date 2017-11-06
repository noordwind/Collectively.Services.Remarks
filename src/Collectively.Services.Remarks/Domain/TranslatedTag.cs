using System;
using System.Collections.Generic;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Domain
{
    public class TranslatedTag : IdentifiableEntity
    {
        public string Name { get; protected set; }
        public string Culture { get; protected set; }

        protected TranslatedTag()
        {
        }

        public TranslatedTag(Guid id, string name, string culture)
        {
            if (name.Empty())
            {
                throw new ArgumentException("Tag name can not be empty.", nameof(name));
            }
            if (culture.Empty())
            {
                throw new ArgumentException("Tag culture can not be empty.", nameof(name));
            }
            Id = id;
            Name = name.TrimToLower().Replace(" ", string.Empty);
            Culture = culture.ToLowerInvariant();
        }
    }
}