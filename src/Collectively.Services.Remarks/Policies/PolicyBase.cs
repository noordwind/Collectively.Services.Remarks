using System;
using Collectively.Common.Domain;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Settings;

namespace Collectively.Services.Remarks.Policies
{
    public abstract class PolicyBase : IPolicy
    {
        private readonly int _secondsBreak;

        protected PolicyBase(int secondsBreak)
        {
            _secondsBreak = secondsBreak;
        }

        protected void Validate(Maybe<ITimestampable> latestResource, 
            string code, string errorMessage)
        {
            if(latestResource.HasNoValue)
            {
                return;
            }
            if(latestResource.Value.CreatedAt.AddSeconds(_secondsBreak) <= DateTime.UtcNow)
            {
                return;
            }
            throw new ServiceException(code, errorMessage);
        }
    }
}