using AutoMapper;
using FluentValidation;
using IamMaturityStudio.Application.DTOs;
using IamMaturityStudio.Application.Validation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace IamMaturityStudio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(typeof(DependencyInjection).Assembly);
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        services.AddTransient<IValidator<AssessmentDto>, AssessmentDtoValidator>();

        return services;
    }
}