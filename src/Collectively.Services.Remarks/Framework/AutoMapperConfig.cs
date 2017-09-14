using AutoMapper;
using System.Linq;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Dto;

namespace Collectively.Services.Remarks.Framework
{
    public class AutoMapperConfig
    {
        public static IMapper InitializeMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Remark, BasicRemarkDto>()
                    .ForMember(x => x.CommentsCount, 
                               m => m.MapFrom(p => p.Comments == null ? 0 : p.Comments.Count()))
                    .ForMember(x => x.ParticipantsCount, 
                               m => m.MapFrom(p => p.Participants == null ? 0 : p.Participants.Count()))
                    .ForMember(x => x.SmallPhotoUrl,
                               m => m.MapFrom(p => p.Photos == null ? 
                                    string.Empty : p.Photos.First(x => x.Size == "small").Url));
                cfg.CreateMap<Remark, RemarkDto>();
                cfg.CreateMap<RemarkState, RemarkStateDto>();
                cfg.CreateMap<RemarkUser, RemarkUserDto>();
                cfg.CreateMap<RemarkCategory, RemarkCategoryDto>();
                cfg.CreateMap<Participant, ParticipantDto>();
                cfg.CreateMap<Comment, CommentDto>();
                cfg.CreateMap<CommentHistory, CommentHistoryDto>();
                cfg.CreateMap<Location, LocationDto>();
                cfg.CreateMap<RemarkPhoto, FileDto>();
                cfg.CreateMap<RemarkGroup, RemarkGroupDto>();
                cfg.CreateMap<Category, RemarkCategoryDto>();
                cfg.CreateMap<Tag, TagDto>();
                cfg.CreateMap<Vote, VoteDto>();
                cfg.CreateMap<Offering, OfferingDto>();
                cfg.CreateMap<OfferingProposal, OfferingProposalDto>();
            });

            return config.CreateMapper();
        }
    }
}