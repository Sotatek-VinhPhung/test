using System.Net;
using System.Net.Http.Json;
using CleanArchitecture.Application.Auth.DTOs;
using CleanArchitecture.Application.Users.DTOs;
using CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Api.Tests;

/// <summary>
/// Custom factory that replaces PostgreSQL with a shared InMemory database per test class.
/// </summary>
public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove EF Core DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}

public class AuthControllerTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturn201()
    {
        var request = new CreateUserRequest("John", "Doe", $"john{Guid.NewGuid():N}@test.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldReturn400()
    {
        var email = $"dup{Guid.NewGuid():N}@test.com";
        var request = new CreateUserRequest("John", "Doe", email, "Password123!");

        var first = await _client.PostAsJsonAsync("/api/auth/register", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/auth/register", request);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200()
    {
        var email = $"login{Guid.NewGuid():N}@test.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new CreateUserRequest("John", "Doe", email, "Password123!"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // AuthService lowercases email on register, so login with lowercase too
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email.ToLowerInvariant(), "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn400()
    {
        var email = $"bad{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new CreateUserRequest("John", "Doe", email, "Password123!"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "WrongPassword!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Users_WithoutAuth_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
