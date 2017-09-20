using System;
using System.Collections.Generic;
using System.Linq;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;
using Collectively.Common.Types;

namespace Collectively.Services.Remarks.Domain
{
    public class Remark : Scorable, ITimestampable
    {
        private ISet<Participant> _participants = new HashSet<Participant>();
        private ISet<RemarkPhoto> _photos = new HashSet<RemarkPhoto>();
        private ISet<RemarkState> _states = new HashSet<RemarkState>();
        private ISet<Comment> _comments = new HashSet<Comment>();
        private ISet<string> _userFavorites = new HashSet<string>();
        private ISet<string> _tags = new HashSet<string>();
        public RemarkUser Author { get; protected set; }
        public RemarkCategory Category { get; protected set; }
        public Location Location { get; protected set; }
        public RemarkState State { get; protected set; }
        public string Description { get; protected set; }
        public RemarkGroup Group { get; protected set; }
        public Offering Offering { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime UpdatedAt { get; protected set; }
        public bool Resolved => State?.State == RemarkState.Names.Resolved;

        public IEnumerable<RemarkPhoto> Photos
        {
            get { return _photos; }
            protected set { _photos = new HashSet<RemarkPhoto>(value); }
        }

        public IEnumerable<RemarkState> States
        {
            get { return _states.OrderBy(s => s.CreatedAt); }
            protected set { _states = new HashSet<RemarkState>(value); }
        }

        public IEnumerable<string> Tags
        {
            get { return _tags; }
            protected set { _tags = new HashSet<string>(value); }
        }

        public IEnumerable<string> UserFavorites
        {
            get { return _userFavorites; }
            protected set { _userFavorites = new HashSet<string>(value); }
        }

        public IEnumerable<Comment> Comments
        {
            get { return _comments; }
            protected set { _comments = new HashSet<Comment>(value); }
        }

        public IEnumerable<Participant> Participants
        {
            get { return _participants; }
            protected set { _participants = new HashSet<Participant>(value); }
        }

        protected Remark()
        {
        }

        public Remark(Guid id, User author, Category category, Location location,
            string description = null, Group group = null)
        {
            Id = id;
            SetAuthor(author);
            SetCategory(category);
            SetLocation(location);
            SetDescription(description);
            SetState(RemarkState.New(Author, location, description));
            Group = group == null ? null : RemarkGroup.Create(group);
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetAuthor(User author)
        {
            if (author == null)
            {
                throw new DomainException(OperationCodes.RemarkAuthorNotProvided,
                    "Remark author can not be null.");
            }
            Author = RemarkUser.Create(author);
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetCategory(Category category)
        {
            if (category == null)
            {
                throw new DomainException(OperationCodes.RemarkCategoryNotProvided,
                    "Remark category can not be null.");
            }
            Category = RemarkCategory.Create(category);
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetLocation(Location location)
        {
            if (location == null)
            {
                throw new DomainException(OperationCodes.RemarkLocationNotProvided, 
                    "Remark location can not be null.");
            }
            Location = location;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddPhoto(RemarkPhoto photo)
        {
            if (photo == null)
            {
                return;
            }
            _photos.Add(photo);
            UpdatedAt = DateTime.UtcNow;
        }
        
        public Maybe<RemarkPhoto> GetPhoto(string name) => Photos.FirstOrDefault(x => x.Name == name);

        public void RemovePhoto(string name)
        {
            var photo = GetPhoto(name);
            if(photo.HasNoValue)
            {
                return;
            }
            _photos.Remove(photo.Value);
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddTag(string tag)
        {
            _tags.Add(tag);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveTag(string tag)
        {
            _tags.Remove(tag);
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetDescription(string description)
        {
            if (description.Empty())
            {
                Description = string.Empty;
                UpdatedAt = DateTime.UtcNow;

                return;
            }
            if (description.Length > 2000)
            {
                throw new DomainException(OperationCodes.InvalidRemarkDescription, 
                    "Remark description is too long.");
            }
            if (Description.EqualsCaseInvariant(description))
            {
                return;
            }
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddComment(Guid id, User user, string text)
        {
            if(_comments.Count >= 1000)
            {
                throw new DomainException(OperationCodes.TooManyComments, 
                    $"Limit of 1000 remark comments was reached.");
            }
            _comments.Add(new Comment(id, user, text));
            UpdatedAt = DateTime.UtcNow;
        }

        public void EditComment(Guid id,  string text)
        {
            var comment = GetCommentOrFail(id);
            comment.Edit(text);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveComment(Guid id)
        {
            var comment = GetCommentOrFail(id);
            _comments.Remove(comment);
            UpdatedAt = DateTime.UtcNow;
        }

        public Comment GetCommentOrFail(Guid id)
        {
            var comment = GetComment(id);
            if(comment.HasNoValue)
            {
                throw new DomainException(OperationCodes.CommentNotFound, 
                    $"Remark comment with id: '{id}' was not found.");
            }

            return comment.Value;
        }

        public Maybe<Comment> GetComment(Guid id)
            => Comments.SingleOrDefault(x => x.Id == id);

        public void AddUserFavorite(User user)
        {
            _userFavorites.Add(user.UserId);
        }

        public void RemoveUserFavorite(User user)
        {
            _userFavorites.Remove(user.UserId);
        }

        public void SetOffering(Offering offering)
        {
            if (offering == null)
            {
                throw new DomainException(OperationCodes.OfferingNotProvided,
                    "Offering can not be null.");
            }
            Offering = offering;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Participate(User user, string description)
        {
            var participant = GetParticipant(user.UserId);
            if (participant.HasValue)
            {
                throw new DomainException(OperationCodes.UserAlreadyParticipatesInRemark, 
                    $"User: '{user.UserId}' already participates in a remark: '{Id}'.");
            }
            _participants.Add(Participant.Create(user, description));
            UpdatedAt = DateTime.UtcNow;
        }

        public void CancelParticipation(string userId)
        {
            var participant = GetParticipant(userId);
            if(participant.HasNoValue)
            {
                return;
            }
            _participants.Remove(participant.Value);
            UpdatedAt = DateTime.UtcNow;
        }

        public Maybe<Participant> GetParticipant(string userId)
            => Participants.SingleOrDefault(x => x.User.UserId == userId);

        public void SetProcessingState(User user, string description = null, 
            RemarkPhoto photo = null)
            => SetState(RemarkState.Processing(RemarkUser.Create(user), description, photo));

        public void SetResolvedState(User user, string description = null, 
            Location location = null, RemarkPhoto photo = null)
            => SetState(RemarkState.Resolved(RemarkUser.Create(user), description, location, photo));

        public void SetRenewedState(User user, string description = null, 
            RemarkPhoto photo = null)
            => SetState(RemarkState.Renewed(RemarkUser.Create(user), description, photo));

        public void SetCanceledState(User user, string description = null)
            => SetState(RemarkState.Canceled(RemarkUser.Create(user), description));

        public Maybe<RemarkState> GetLatestStateOf(string state) 
            => States.OrderByDescending(x => x.CreatedAt).FirstOrDefault(x => x.State == state);

        public RemarkState GetStateOrFail(Guid id)
        {
            var state = GetState(id);
            if (state.HasNoValue)
            {
                throw new DomainException(OperationCodes.StateNotFound, "Cannot find state." +
                    $" remarkId: {Id}, stateId: {id}");
            }
            return state.Value;
        }

        public Maybe<RemarkState> GetState(Guid id)
            => States.SingleOrDefault(s => s.Id == id);

        public void RemoveState(Guid id)
        {
            var state = GetStateOrFail(id);
            state.Remove();

            var isLatest = _states
                .OrderBy(s => s.CreatedAt)
                .LastOrDefault()?.Id == state.Id;

            if (isLatest == false)
                return;

            var previousState = _states
                .Reverse()
                .Skip(1)
                .FirstOrDefault(s => s.Removed == false);

            if (previousState != null)
            {
                State = previousState;
            }
        }

        private void SetState(RemarkState state)
        {
            if(state == null)
            {
                throw new DomainException(OperationCodes.RemarkStateNotProvided, 
                    "Remark state can not be null.");
            }

            var latestState = _states.LastOrDefault();
            if(latestState == null)
            {
                _states.Add(state);
                State = state;

                return;
            }
            if(latestState.State == RemarkState.Names.Canceled)
            {
                throw new DomainException(OperationCodes.CannotSetState,
                    $"Can not set state to '{state}' for remark with id: '{Id}'" +
                     "as it was canceled.");                
            }
            if(latestState.State == state.State &&  latestState.State != RemarkState.Names.Processing)
            {
                throw new DomainException(OperationCodes.CannotSetState,
                    $"Can not set state to '{state}' for remark with id: '{Id}'" +
                     "as it's the same as the previous one.");
            }
            _states.Add(state);
            State = state;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}