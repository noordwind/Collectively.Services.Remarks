using System;
using System.Collections.Generic;
using Coolector.Common.Commands;
using Coolector.Services.Remarks.Shared.Commands.Models;

namespace Coolector.Services.Remarks.Shared.Commands
{
    public class AddPhotosToRemark : IAuthenticatedCommand
    {
        public Guid RemarkId { get; set; }
        public Request Request { get; set; }
        public string UserId { get; set; }
        public IList<RemarkFile> Photos { get; set; }
    }
}