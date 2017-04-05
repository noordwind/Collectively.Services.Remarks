using System.Collections.Generic;

namespace Collectively.Services.Remarks.Dto
{
    public class RemarkDto : BasicRemarkDto
    {
        public IList<FileDto> Photos { get; set; }
        public IList<RemarkStateDto> States { get; set; }
        public IList<string> Tags { get; set; }
        public IList<VoteDto> Votes { get; set; }
        public IList<CommentDto> Comments { get; set; }
        public IList<string> UserFavorites { get; set; }
        public IList<ParticipantDto> Participants { get; set; }
    }
}