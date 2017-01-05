using System;
using Coolector.Common.Commands;

namespace Coolector.Services.Remarks.Shared.Commands
{
    public class SubmitRemarkVote : IAuthenticatedCommand
    {
        public Guid RemarkId { get; set; }
        public Request Request { get; set; }
        public string UserId { get; set; }
        public bool Positive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}