using System;
using Coolector.Common.Domain;
using Coolector.Common.Extensions;

namespace Coolector.Services.Remarks.Domain
{
    public class LocalizedResource : IdentifiableEntity
    {
        public string Name { get; protected set; }
        public string Culture { get; protected set; }
        public string Text { get; protected set; }

        protected LocalizedResource()
        {
        }

        public LocalizedResource(string name, string culture, string text)
        {
            Id = Guid.NewGuid();
            SetName(name);
            SetCulture(culture);
            SetText(text);
        }

        public void SetName(string name)
        {
            if(name.Empty())
            {
                throw new ArgumentException("Name can not be empty.", nameof(name));
            }

            Name = name.ToLowerInvariant();
        }

        public void SetCulture(string culture)
        {
            if(culture.Empty())
            {
                throw new ArgumentException("Culture can not be empty.", nameof(culture));
            }
            
            Culture = culture.ToLowerInvariant();
        }

        public void SetText(string text)
        {
            if(text.Empty())
            {
                throw new ArgumentException("Text can not be empty.", nameof(text));
            }
            
            Text = text;
        }

        public string GetTranslatedText(params object[] args)
            => string.Format(Text, args);
    }
}