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
        public DateTime CreatedAt { get; protected set; }
        public bool Resolved => State?.State == RemarkState.Names.Resolved;

        public IEnumerable<RemarkPhoto> Photos
        {
            get { return _photos; }
            protected set { _photos = new HashSet<RemarkPhoto>(value); }
        }

        public IEnumerable<RemarkState> States
        {
            get { return _states; }
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
            string description = null)
        {
            Id = id;
            SetAuthor(author);
            SetCategory(category);
            SetLocation(location);
            SetDescription(description);
            SetState(RemarkState.New(Author, location, description));
            CreatedAt = DateTime.UtcNow;
        }

        public void SetAuthor(User author)
        {
            if (author == null)
            {
                throw new DomainException(OperationCodes.RemarkAuthorNotProvided,
                    "Remark author can not be null.");
            }
            Author = RemarkUser.Create(author);
        }

        public void SetCategory(Category category)
        {
            if (category == null)
            {
                throw new DomainException(OperationCodes.RemarkCategoryNotProvided,
                    "Remark category can not be null.");
            }
            Category = RemarkCategory.Create(category);
        }

        public void SetLocation(Location location)
        {
            if (location == null)
            {
                throw new DomainException(OperationCodes.RemarkLocationNotProvided, 
                    "Remark location can not be null.");
            }
            Location = location;
        }

        public void AddPhoto(RemarkPhoto photo)
        {
            if (photo == null)
            {
                return;
            }
            _photos.Add(photo);
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
        }

        public void AddTag(string tag)
        {
            _tags.Add(tag);
        }

        public void RemoveTag(string tag)
        {
            _tags.Remove(tag);
        }

        public void SetDescription(string description)
        {
            if (description.Empty())
            {
                Description = string.Empty;

                return;
            }
            if (description.Length > 500)
            {
                throw new DomainException(OperationCodes.InvalidRemarkDescription, 
                    "Remark description is too long.");
            }
            if (Description.EqualsCaseInvariant(description))
            {
                return;
            }
            Description = description;
        }

        public void AddComment(Guid id, User user, string text)
        {
            if(_comments.Count >= 500)
            {
                throw new DomainException(OperationCodes.TooManyComments, 
                    $"Limit of 500 remark comments was reached.");
            }
            _comments.Add(new Comment(id, user, text));
        }

        public void EditComment(Guid id,  string text)
        {
            var comment = GetCommentOrFail(id);
            comment.Edit(text);
        }

        public void RemoveComment(Guid id)
        {
            var comment = GetCommentOrFail(id);
            _comments.Remove(comment);
        }

        public Maybe<Comment> GetComment(Guid id)
            => Comments.FirstOrDefault(x => x.Id == id);

        private Comment GetCommentOrFail(Guid id)
        {
            var comment = GetComment(id);
            if(comment.HasNoValue)
            {
                throw new DomainException(OperationCodes.CommentNotFound, 
                    $"Remark comment with id: '{id}' was not found.");
            }

            return comment.Value;
        }

        public void AddUserFavorite(User user)
        {
            _userFavorites.Add(user.UserId);
        }

        public void RemoveUserFavorite(User user)
        {
            _userFavorites.Remove(user.UserId);
        }

        public void Participate(User user, string description)
        {
            _participants.Add(Participant.Create(user, description));
        }

        public void CancelParticipation(string userId)
        {
            var participant = _participants.FirstOrDefault(x => x.User.UserId == userId);
            if(participant == null)
            {
                return;
            }
            _participants.Remove(participant);
        }

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
        }
    }
}