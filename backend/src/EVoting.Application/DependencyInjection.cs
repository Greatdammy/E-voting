using System.Reflection;
using EVoting.Application.Interfaces;
using EVoting.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EVoting.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IElectionService, ElectionService>();
        services.AddScoped<ICandidateService, CandidateService>();
        services.AddScoped<IVoteService, VoteService>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
