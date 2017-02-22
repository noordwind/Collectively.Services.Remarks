using System;
using System.Collections.Generic;
using Coolector.Services.Remarks.Shared.Events.Models;

namespace Coolector.Services.Remarks.Shared.Events
{
    public class RemarkResolved : RemarkStateChanged
    {
        public IEnumerable<RemarkFile> Photos { get; }

        protected RemarkResolved()
        {
        }

        public RemarkResolved(Guid requestId, Guid remarkId, 
            string userId, string username, string description, 
            RemarkLocation location, DateTime createdAt, 
            IEnumerable<RemarkFile> photos) 
            : base(requestId, remarkId, userId, username, 
                "resolved",description, location, createdAt)
        {
            Photos = photos;
        }
    }
}