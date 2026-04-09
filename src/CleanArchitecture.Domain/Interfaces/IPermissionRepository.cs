using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Permission data access contract.
/// </summary>
public interface IPermissionRepository
{
    Task<IReadOnlyList<RolePermission>> GetByRoleAsync(
        Role role, CancellationToken ct = default);

    Task<IReadOnlyList<UserPermissionOverride>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default);

    Task<long> GetEffectiveFlagsAsync(
        Guid userId, Role role, string module, CancellationToken ct = default);

    Task UpsertRolePermissionAsync(RolePermission permission, CancellationToken ct = default);
    Task UpsertUserOverrideAsync(UserPermissionOverride overrideEntity, CancellationToken ct = default);
}
