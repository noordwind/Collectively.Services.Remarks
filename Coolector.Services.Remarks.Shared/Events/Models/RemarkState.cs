using System;

namespace Coolector.Services.Remarks.Shared.Events.Models
{
    public class RemarkState
    {
        public string State { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Description { get; set; }
        public RemarkLocation Location { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}