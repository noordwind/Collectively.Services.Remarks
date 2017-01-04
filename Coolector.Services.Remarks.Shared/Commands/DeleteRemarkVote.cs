using System;
using Coolector.Common.Commands;

namespace Coolector.Services.Shared.Commands
{
    public class DeleteRemarkVote : IAuthenticatedCommand
    {
        public Guid RemarkId { get; set; }
        public Request Request { get; set; }
        public string UserId { get; set; }
    }
}