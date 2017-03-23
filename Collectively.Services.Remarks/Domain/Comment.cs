using System;
using System.Collections.Generic;
using System.Linq;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;

namespace Collectively.Services.Remarks.Domain
{
    public class Comment : Scorable, ITimestampable
    {
        private ISet<CommentHistory> _history = new HashSet<CommentHistory>();
        public RemarkUser User { get; protected set; }
        public string Text { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public bool Removed => RemovedAt.HasValue;
        public DateTime? RemovedAt { get; protected set; }

        public IEnumerable<CommentHistory> History
        {
            get { return _history; }
            protected set { _history = new HashSet<CommentHistory>(value); }
        }

        protected Comment()
        {
        }

        public Comment(Guid id, User user, string text)
        {
            Id = id;
            SetUser(user);
            SetText(text);
            CreatedAt = DateTime.UtcNow;
        }

        public void Edit(string text)
        {
            if(Removed)
            {
                throw new DomainException(OperationCodes.CommentRemoved,
                    $"Comment: '{Id}' was removed at {RemovedAt}.");
            }
            if(History.Count() >= 5)
            {
                throw new DomainException(OperationCodes.CommentEditedTooManyTimes,
                    $"Comment: '{Id}' can not be edited more than 5 times.");
            }
            ValidateText(text);
            _history.Add(CommentHistory.Create(text));
        }

        public void Remove()
        {
            if(Removed)
            {
                throw new DomainException(OperationCodes.CommentRemoved,
                    $"Comment: '{Id}' was removed at {RemovedAt}.");
            }
            Text = string.Empty;
            _history.Clear();
            RemovedAt = DateTime.UtcNow;
        }

        private void SetText(string text)
        {
            ValidateText(text);
            Text = text.Trim();
        }

        private void ValidateText(string text)
        {
            if(text.Empty())
            {
                throw new DomainException(OperationCodes.EmptyComment,
                    $"Comment: '{Id}' can not be empty.");
            }
            if(text.Length > 1000)
            {
                throw new DomainException(OperationCodes.EmptyComment,
                    $"Comment: '{Id}' is too long ({text.Length} characters).");
            }
        }

        private void SetUser(User user)
        {
            if (user == null)
            {
                throw new DomainException(OperationCodes.CommentUserNotProvided,
                    $"Comment: '{Id}' user can not be null.");
            }
            User = RemarkUser.Create(user);
        }
    }
}