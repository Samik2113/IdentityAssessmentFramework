using AutoMapper;
using IamMaturityStudio.Application.DTOs;
using IamMaturityStudio.Domain.Entities;

namespace IamMaturityStudio.Application.Mappings;

public class AssessmentMappingProfile : Profile
{
    public AssessmentMappingProfile()
    {
        CreateMap<Assessment, AssessmentDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}