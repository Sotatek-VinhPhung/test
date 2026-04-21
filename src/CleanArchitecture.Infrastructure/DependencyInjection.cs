using System.Text;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Permissions.Interfaces;
using CleanArchitecture.Application.Permissions.Services;
using CleanArchitecture.Application.Permissions.Helpers;
using CleanArchitecture.Application.Notifications.Interfaces;
using CleanArchitecture.Application.Export.Interfaces;
using CleanArchitecture.Application.Export.Services;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Domain.Interfaces.Export;
using CleanArchitecture.Infrastructure.Auth;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Persistence.Services;
using CleanArchitecture.Infrastructure.Persistence.Repositories;
using CleanArchitecture.Infrastructure.Permissions;
using CleanArchitecture.Infrastructure.Notifications;
using CleanArchitecture.Infrastructure.FileStorage;
using CleanArchitecture.Infrastructure.FileGeneration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Minio;

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
        services.AddSingleton<IExcelTemplateEngine, ExcelTemplateEngine>();
        services.AddSingleton<IWordTemplateEngine, WordTemplateEngine>();
        // Permission services
        services.AddScoped<IUserContextService, UserContextServiceImpl>();
        services.AddScoped<PermissionChecker>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IHierarchicalPermissionService, HierarchicalPermissionService>();

        // Notification services
        services.AddScoped<IPermissionNotificationService, PermissionNotificationService>();

        // Export feature - Repositories
        services.AddScoped<IExportedFileRepository, ExportedFileRepository>();

        // Export feature - File generation services
        services.AddScoped<IExcelFileGenerator, ExcelFileGenerator>();
        services.AddScoped<IWordFileGenerator, WordFileGenerator>();
        services.AddScoped<IPdfFileGenerator, PdfFileGenerator>();

        // Gotenberg
        services.Configure<GotenbergSettings>(
            configuration.GetSection(GotenbergSettings.SectionName));
        var gotenbergSettings = configuration
        .GetSection(GotenbergSettings.SectionName)
        .Get<GotenbergSettings>() ?? new GotenbergSettings();

        services.AddHttpClient<IGotenbergService, GotenbergService>(client =>
        {
            var baseUrl = gotenbergSettings.BaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(gotenbergSettings.TimeoutSeconds);
        });

        // Export feature - File storage service (MinIO)
        var minioSettings = configuration.GetSection("MinIO");
        var minioEndpoint = minioSettings.GetValue<string>("Endpoint") ?? "localhost:9000";
        var minioAccessKey = minioSettings.GetValue<string>("AccessKey") ?? "minioadmin";
        var minioSecretKey = minioSettings.GetValue<string>("SecretKey") ?? "minioadmin";
        var minioBucket = minioSettings.GetValue<string>("Bucket") ?? "exports";
        services.AddScoped<IMinioClient>(sp =>
        {
            var minioClient = new MinioClient()
                .WithEndpoint(minioEndpoint)
                .WithCredentials(minioAccessKey, minioSecretKey)
                .Build();
            return minioClient;
        });

        services.AddScoped<IFileStorageService>(sp =>
        {
            var minioClient = sp.GetRequiredService<IMinioClient>();
            var baseUrl = minioSettings.GetValue<string>("BaseUrl") ?? $"http://{minioEndpoint}";
            return new MinIOFileStorageService(minioClient, baseUrl);
        });

        // Export feature - Export service
        services.AddScoped(sp =>
        {
            var repository = sp.GetRequiredService<IExportedFileRepository>();
            var storage = sp.GetRequiredService<IFileStorageService>();
            var excel = sp.GetRequiredService<IExcelFileGenerator>();
            var word = sp.GetRequiredService<IWordFileGenerator>();
            var pdf = sp.GetRequiredService<IPdfFileGenerator>();
            var currentUserService = sp.GetRequiredService<ICurrentUserService>();
            var templateEngine = sp.GetRequiredService<IExcelTemplateEngine>();
            var wordTemplateEngine = sp.GetRequiredService<IWordTemplateEngine>();
            var gotenbergService = sp.GetRequiredService<IGotenbergService>();
            return new ExportService(repository, storage, excel, word, pdf, currentUserService, templateEngine, wordTemplateEngine, gotenbergService, "Templates", minioBucket);
        });

        services.AddScoped<IExportService>(sp => sp.GetRequiredService<ExportService>());

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
