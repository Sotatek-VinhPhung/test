using System.Text;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Permissions.Interfaces;
using CleanArchitecture.Application.Permissions.Services;
using CleanArchitecture.Application.Permissions.Helpers;
using CleanArchitecture.Application.Notifications.Interfaces;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Infrastructure.Auth;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Persistence.Services;
using CleanArchitecture.Infrastructure.Persistence.Repositories;
using CleanArchitecture.Infrastructure.Permissions;
using CleanArchitecture.Infrastructure.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CleanArchitecture.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Database - PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories & UoW
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
#pragma warning disable CS0618 // Type or member is obsolete
        services.AddScoped<IPermissionRepository, PermissionRepository>(); // Obsolete: legacy only
#pragma warning restore CS0618 // Type or member is obsolete
        services.AddScoped<RoleRepository>();
        services.AddScoped<SubsystemRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>(); // New RBAC
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Permission services
        services.AddScoped<IUserContextService, UserContextServiceImpl>();
        services.AddScoped<PermissionChecker>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();

        // Notification services
        services.AddScoped<IPermissionNotificationService, PermissionNotificationService>();

        // Auth
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        return services;
    }
}
