using FluentValidation;
using IamMaturityStudio.Application.DTOs;

namespace IamMaturityStudio.Application.Validation;

public class AssessmentDtoValidator : AbstractValidator<AssessmentDto>
{
    public AssessmentDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
    }
}