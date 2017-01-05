using System;
using Coolector.Common.Commands;

namespace Coolector.Services.Remarks.Shared.Commands
{
    public class DeleteRemarkVote : IAuthenticatedCommand
    {
        public Request Request { get; set; }
        public string UserId { get; set; }
        public Guid RemarkId { get; set; }
    }
}