using System;
using System.Collections.Generic;
using System.Linq;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Domain
{
    public abstract class Scorable : IdentifiableEntity, IScorable
    {
        private ISet<Vote> _votes = new HashSet<Vote>();
        public int Rating { get; protected set; }
        public IEnumerable<Vote> Votes
        {
            get { return _votes; }
            protected set { _votes = new HashSet<Vote>(value); }
        }

        public void VotePositive(string userId, DateTime createdAt)
        {
            if (Votes.Any(x => x.UserId == userId && x.Positive))
            {
                throw new DomainException(OperationCodes.CannotSubmitVote,
                    $"User with id: '{userId}' has already " + 
                    $"submitted a positive vote for {this.GetType().Name} with id: '{Id}''.");
            }
            var negativeVote = Votes.SingleOrDefault(x => x.UserId == userId && !x.Positive);
            if (negativeVote != null)
            {
                _votes.Remove(negativeVote);
                Rating++;
            }

            _votes.Add(Vote.GetPositive(userId, createdAt));
            Rating++;
        }

        public void VoteNegative(string userId, DateTime createdAt)
        {
            if (Votes.Any(x => x.UserId == userId && !x.Positive))
            {
                throw new DomainException(OperationCodes.CannotSubmitVote,
                    $"User with id: '{userId}' has already " + 
                    $"submitted a negative vote for {this.GetType().Name} with id: '{Id}'.");
            }
            var positiveVote = Votes.SingleOrDefault(x => x.UserId == userId && x.Positive);
            if (positiveVote != null)
            {
                _votes.Remove(positiveVote);
                Rating--;
            }

            _votes.Add(Vote.GetNegative(userId, createdAt));
            Rating--;
        }

        public void DeleteVote(string userId)
        {
            var vote = Votes.SingleOrDefault(x => x.UserId == userId);
            if (vote == null)
            {
                throw new DomainException(OperationCodes.CannotDeleteVote, 
                    $"User with id: '{userId}' has not " + 
                    $"submitted any vote for {this.GetType().Name} with id: '{Id}'.");              
            }
            if (vote.Positive)
            {
                Rating--;
            }
            else
            {
                Rating++;
            }
            _votes.Remove(vote);
        }
    }
}