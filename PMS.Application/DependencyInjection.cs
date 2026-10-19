using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PMS.Application.Services.Interfaces;
using PMS.Application.Services.Implementations;

namespace PMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<PMS.Application.Common.Interfaces.IAccessScope, PMS.Application.Common.AccessScope>();
        // A host with a real hub registers its own IChatAccessSync before this call; this is the fallback.
        services.TryAddSingleton<PMS.Application.Common.Interfaces.IChatAccessSync, PMS.Application.Common.Interfaces.NullChatAccessSync>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IMasterService, MasterService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskListService, TaskListService>();
        services.AddScoped<IIssueService, IssueService>();
        services.AddScoped<IMilestoneService, MilestoneService>();
        services.AddScoped<ITimeLogService, TimeLogService>();
        services.AddScoped<ITimesheetService, TimesheetService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IProjectGroupService, ProjectGroupService>();
        services.AddScoped<IProjectTemplateService, ProjectTemplateService>();
        services.AddScoped<IAuditService, AuditService>();
        return services;
    }
}
