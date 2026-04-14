using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Infrastructure.Persistence.Repositories;

namespace CleanArchitecture.Infrastructure.Persistence;

/// <summary>
/// Unit of Work pattern implementation - DEPRECATED for Permissions.
/// Using new RBAC system repositories directly from DI container instead.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IUserRepository? _users;
#pragma warning disable CS0618 // Type or member is obsolete
    private IPermissionRepository? _permissions;
#pragma warning restore CS0618 // Type or member is obsolete

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
#pragma warning disable CS0618 // Type or member is obsolete
    public IPermissionRepository Permissions => _permissions ??= new PermissionRepository(_context);
#pragma warning restore CS0618 // Type or member is obsolete

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
