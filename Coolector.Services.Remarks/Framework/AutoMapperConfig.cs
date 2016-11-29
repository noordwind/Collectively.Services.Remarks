using AutoMapper;
using Coolector.Common.Dto.General;
using Coolector.Common.Dto.Remarks;
using Coolector.Services.Remarks.Domain;

namespace Coolector.Services.Remarks.Framework
{
    public class AutoMapperConfig
    {
        public static IMapper InitializeMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Remark, RemarkDto>();
                cfg.CreateMap<RemarkAuthor, RemarkAuthorDto>();
                cfg.CreateMap<RemarkCategory, RemarkCategoryDto>();
                cfg.CreateMap<Location, LocationDto>();
                cfg.CreateMap<RemarkPhoto, FileDto>();
                cfg.CreateMap<Category, RemarkCategoryDto>();
            });

            return config.CreateMapper();
        }
    }
}