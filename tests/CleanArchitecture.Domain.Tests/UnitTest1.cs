using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Exceptions;
using FluentAssertions;

namespace CleanArchitecture.Domain.Tests;

public class UserEntityTests
{
    [Fact]
    public void User_ShouldInitialize_WithDefaultValues()
    {
        var user = new User();

        user.Id.Should().Be(Guid.Empty);
        user.FirstName.Should().BeEmpty();
        user.LastName.Should().BeEmpty();
        user.Email.Should().BeEmpty();
        user.Role.Should().Be(CleanArchitecture.Domain.Enums.Role.User);
        user.RefreshToken.Should().BeNull();
    }

    [Fact]
    public void User_ShouldSetProperties_Correctly()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PasswordHash = "hashed",
            Role = CleanArchitecture.Domain.Enums.Role.Admin
        };

        user.Id.Should().Be(id);
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Email.Should().Be("john@example.com");
        user.Role.Should().Be(CleanArchitecture.Domain.Enums.Role.Admin);
    }
}

public class NotFoundExceptionTests
{
    [Fact]
    public void NotFoundException_ShouldContain_EntityNameAndKey()
    {
        var key = Guid.NewGuid();
        var ex = new NotFoundException("User", key);

        ex.Message.Should().Contain("User");
        ex.Message.Should().Contain(key.ToString());
    }
}
