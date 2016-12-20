using System;
using System.Collections.Generic;
using Coolector.Common.Events;
using Coolector.Services.Remarks.Shared.Events.Models;

namespace Coolector.Services.Remarks.Shared.Events
{
    public class PhotosToRemarkAdded : IAuthenticatedEvent
    {
        public Guid RequestId { get; }
        public Guid RemarkId { get; }
        public string UserId { get; }
        public IEnumerable<RemarkFile> Photos { get; }

        protected PhotosToRemarkAdded()
        {
        }

        public PhotosToRemarkAdded(Guid requestId, Guid remarkId, 
            string userId, IEnumerable<RemarkFile> photos)
        {
            RequestId = requestId;
            RemarkId = remarkId;
            UserId = userId;
            Photos = photos;
        }
    }
}