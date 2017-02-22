using Coolector.Services.Remarks.Shared.Commands.Models;

namespace Coolector.Services.Remarks.Shared.Commands
{
    public class ResolveRemark : ChangeRemarkStateBase
    {
        public RemarkFile Photo { get; set; }
        public bool ValidatePhoto { get; set; }
        public bool ValidateLocation { get; set; }
    }
}