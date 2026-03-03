using AutoMapper;
using IamMaturityStudio.Application.DTOs;
using IamMaturityStudio.Application.Interfaces;
using MediatR;

namespace IamMaturityStudio.Application.UseCases.Assessments;

public record GetAssessmentsQuery : IRequest<IReadOnlyList<AssessmentDto>>;

public class GetAssessmentsQueryHandler : IRequestHandler<GetAssessmentsQuery, IReadOnlyList<AssessmentDto>>
{
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IMapper _mapper;

    public GetAssessmentsQueryHandler(IAssessmentRepository assessmentRepository, IMapper mapper)
    {
        _assessmentRepository = assessmentRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AssessmentDto>> Handle(GetAssessmentsQuery request, CancellationToken cancellationToken)
    {
        var assessments = await _assessmentRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyList<AssessmentDto>>(assessments);
    }
}