using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PMS.Application.Common.Interfaces;
using PMS.Infrastructure.Data;

namespace PMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, b => b
                .MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                .MigrationsHistoryTable("__EFMigrationsHistory", PMS.Domain.Constants.AppConstants.PmsSchema)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ITokenService, PMS.Infrastructure.Services.TokenService>();
        services.AddSingleton<IPasswordHasher, PMS.Infrastructure.Services.PasswordHasher>();
        services.AddSingleton<IBlobStorageService, PMS.Infrastructure.Services.BlobStorageService>();
        services.AddSingleton<ICredentialProtector, PMS.Infrastructure.Services.AesGcmCredentialProtector>();

        return services;
    }
}
