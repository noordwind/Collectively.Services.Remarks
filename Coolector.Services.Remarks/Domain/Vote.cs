using System;
using Coolector.Common.Domain;
using Coolector.Common.Extensions;

namespace Coolector.Services.Remarks.Domain
{
    public class Vote : ValueObject<Vote>
    {
        public string UserId { get; protected set; }
        public bool Positive { get; protected set; }
        public DateTime CreatedAt { get; protected set; }

        protected Vote()
        {
        }

        protected Vote(string userId, bool positive)
        {
            if (userId.Empty())
            {
                throw new ArgumentException("User id can not be empty.", nameof(userId));
            }

            UserId = userId;
            Positive = positive;
            CreatedAt = DateTime.UtcNow;
        }

        public static Vote GetNegative(string userId) => new Vote(userId, false);
        public static Vote GetPositive(string userId) => new Vote(userId, true);

        protected override bool EqualsCore(Vote other) 
            => UserId.Equals(other.UserId) && Positive.Equals(other.Positive);

        protected override int GetHashCodeCore() => UserId.GetHashCode() + Positive.GetHashCode();
    }
}