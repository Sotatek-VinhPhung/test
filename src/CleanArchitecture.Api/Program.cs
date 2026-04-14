using CleanArchitecture.Application;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Infrastructure.Caching;
using CleanArchitecture.Infrastructure.Messaging;
using CleanArchitecture.Api.Extensions;
using CleanArchitecture.Api.Authorization;
using CleanArchitecture.Api.Middleware;
using CleanArchitecture.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Layer DI registration
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddKafkaServices(builder.Configuration, typeof(CleanArchitecture.Application.Auth.Services.AuthService).Assembly);
builder.Services.AddCacheServices(builder.Configuration);

// SignalR
builder.Services.AddSignalR();

// Register factory for IHubContext that will be resolved after app.Build()
builder.Services.AddScoped(sp =>
{
    // This will work after the app is built and hubs are available
    try
    {
        var hubContextType = typeof(IHubContext<>).MakeGenericType(typeof(PermissionNotificationHub));
        var hubContext = sp.GetService(hubContextType);
        return (dynamic)(hubContext ?? new object());
    }
    catch
    {
        // Return a dummy object if hub context is not available yet
        return (dynamic)new object();
    }
});

// Permission authorization
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Controllers
builder.Services.AddControllers();

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CleanArchitecture API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR hubs
app.MapHub<PermissionNotificationHub>("/hubs/permissions");

// Apply migrations and seed RBAC system data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CleanArchitecture.Infrastructure.Persistence.AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await app.SeedRbacAsync();
}

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
