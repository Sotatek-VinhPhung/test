namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern - coordinates repository saves in a single transaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IPermissionRepository Permissions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
