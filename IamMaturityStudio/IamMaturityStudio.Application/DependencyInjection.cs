using AutoMapper;
using FluentValidation;
using IamMaturityStudio.Application.DTOs;
using IamMaturityStudio.Application.Interfaces;
using IamMaturityStudio.Application.Services;
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
        services.AddValidatorsFromAssemblyContaining<AssessmentDtoValidator>();
        services.AddScoped<IScoringService, ScoringService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAiGuidanceService, AiGuidanceService>();

        return services;
    }
}