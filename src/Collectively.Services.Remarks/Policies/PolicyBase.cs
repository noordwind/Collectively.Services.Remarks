using System;
using Collectively.Common.Domain;
using Collectively.Common.Types;

namespace Collectively.Services.Remarks.Policies
{
    public abstract class PolicyBase : IPolicy
    {
        private readonly int _minutesBreak;

        protected PolicyBase(int minutesBreak = 1)
        {
            _minutesBreak = minutesBreak;
        }

        protected void Validate(Maybe<ITimestampable> latestResource, 
            string code, string errorMessage)
        {
            if(latestResource.HasNoValue)
            {
                return;
            }
            if(latestResource.Value.CreatedAt.AddMinutes(_minutesBreak) <= DateTime.UtcNow)
            {
                return;
            }
            throw new ServiceException(code, errorMessage);
        }
    }
}