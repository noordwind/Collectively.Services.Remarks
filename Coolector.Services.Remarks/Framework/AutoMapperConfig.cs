using AutoMapper;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Shared.Dto;

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