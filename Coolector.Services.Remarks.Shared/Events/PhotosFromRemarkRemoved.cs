using System;
using System.Collections.Generic;
using Coolector.Common.Events;
using Coolector.Services.Remarks.Shared.Events.Models;

namespace Coolector.Services.Remarks.Shared.Events
{
    public class PhotosFromRemarkRemoved : IAuthenticatedEvent
    {
        public Guid RequestId { get; }
        public Guid RemarkId { get; }
        public string UserId { get; }
        public IEnumerable<string> Photos { get; }

        protected PhotosFromRemarkRemoved()
        {
        }

        public PhotosFromRemarkRemoved(Guid requestId, Guid remarkId, 
            string userId, IEnumerable<string> photos)
        {
            RequestId = requestId;
            RemarkId = remarkId;
            UserId = userId;
            Photos = photos;
        }
    }
}