using System;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public class CommentHistory : ValueObject<CommentHistory>
    {
        public string Text { get; protected set; }
        public DateTime CreatedAt { get; protected set; }

        protected CommentHistory()
        {
        }

        protected CommentHistory(string text)
        {
            Text = text.Trim();
            CreatedAt = DateTime.UtcNow;
        }

        public static CommentHistory Create(string text)
            => new CommentHistory(text);

        protected override bool EqualsCore(CommentHistory other)
            => Text == other.Text && CreatedAt == other.CreatedAt;
        protected override int GetHashCodeCore()
            => Text.GetHashCode() ^ CreatedAt.GetHashCode();
    }
}