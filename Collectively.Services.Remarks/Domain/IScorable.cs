using System.Collections.Generic;

namespace Collectively.Services.Remarks.Domain
{
    public interface IScorable
    {
        int Rating { get; }
        IEnumerable<Vote> Votes { get; }
    }
}