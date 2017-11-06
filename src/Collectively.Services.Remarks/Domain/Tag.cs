using System;
using System.Collections.Generic;
using System.Linq;
using  Collectively.Common.Domain;
using  Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Domain
{
    public class Tag : IdentifiableEntity
    {
        private ISet<TranslatedTag> _translations = new HashSet<TranslatedTag>();
        public string Name { get; protected set; }
        public IEnumerable<TranslatedTag> Translations
        {
            get { return _translations; }
            protected set { _translations = new HashSet<TranslatedTag>(value); }
        }

        protected Tag()
        {
        }

        public Tag(Guid id, string name, IEnumerable<TranslatedTag> translations)
        {
            if (name.Empty())
            {
                throw new ArgumentException("Tag name can not be empty.", nameof(name));
            }
            Id = id;
            Name = name.TrimToLower().Replace(" ", string.Empty);
            Translations = Enumerable.Empty<TranslatedTag>();
            AddTranslations(translations);
        }

        public void AddTranslations(IEnumerable<TranslatedTag> translations)
        {
            foreach (var tag in translations)
            {
                if (Translations.Any(x => x.Culture == tag.Culture))
                {
                    continue;
                }
                _translations.Add(tag);
            }
        }
    }
}