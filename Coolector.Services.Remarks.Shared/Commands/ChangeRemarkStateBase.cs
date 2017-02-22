using System;
using Coolector.Common.Commands;

namespace Coolector.Services.Remarks.Shared.Commands
{
    public class ChangeRemarkStateBase : IAuthenticatedCommand
    {
        public Request Request { get; set; }
        public string UserId { get; set; }
        public Guid RemarkId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
    }
}