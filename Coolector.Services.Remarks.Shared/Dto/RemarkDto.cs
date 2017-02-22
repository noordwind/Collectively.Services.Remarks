using System.Collections.Generic;

namespace Coolector.Services.Remarks.Shared.Dto
{
    public class RemarkDto : BasicRemarkDto
    {
        public IList<FileDto> Photos { get; set; }
        public IList<RemarkStateDto> States { get; set; }
        public IList<string> Tags { get; set; }
        public IList<VoteDto> Votes { get; set; }
    }
}