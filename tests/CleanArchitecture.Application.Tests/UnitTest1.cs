using CleanArchitecture.Application.Users.DTOs;
using CleanArchitecture.Application.Users.Mappings;
using CleanArchitecture.Application.Users.Services;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace CleanArchitecture.Application.Tests;

public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IUserRepository> _userRepo;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _userRepo = new Mock<IUserRepository>();
        _unitOfWork.Setup(u => u.Users).Returns(_userRepo.Object);
        _sut = new UserService(_unitOfWork.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@test.com", PasswordHash = "h" },
            new() { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", Email = "jane@test.com", PasswordHash = "h" }
        };
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var result = await _sut.GetAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, FirstName = "John", LastName = "Doe", Email = "john@test.com", PasswordHash = "h" };
        _userRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.GetByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserNotFound_ShouldThrowNotFoundException()
    {
        var id = Guid.NewGuid();
        _userRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => _sut.GetByIdAsync(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAndSave()
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, FirstName = "Old", LastName = "Name", Email = "old@test.com", PasswordHash = "h" };
        _userRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.UpdateAsync(id, new UpdateUserRequest("New", "Name"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("New");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteAndSave()
    {
        var id = Guid.NewGuid();
        var user = new User { Id = id, FirstName = "John", LastName = "Doe", Email = "john@test.com", PasswordHash = "h" };
        _userRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _sut.DeleteAsync(id);

        result.IsSuccess.Should().BeTrue();
        _userRepo.Verify(r => r.Delete(user), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class UserMappingsTests
{
    [Fact]
    public void ToDto_ShouldMapCorrectly()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            PasswordHash = "hash",
            Role = CleanArchitecture.Domain.Enums.Role.Admin
        };

        var dto = user.ToDto();

        dto.Id.Should().Be(user.Id);
        dto.FirstName.Should().Be("John");
        dto.Email.Should().Be("john@test.com");
        dto.Role.Should().Be("Admin");
    }
}
