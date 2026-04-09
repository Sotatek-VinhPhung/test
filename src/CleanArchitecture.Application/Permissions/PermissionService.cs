using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;

namespace CleanArchitecture.Application.Permissions;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;

    public PermissionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId, Role role, string module, long requiredFlags,
        CancellationToken ct = default)
    {
        var effective = await _unitOfWork.Permissions
            .GetEffectiveFlagsAsync(userId, role, module, ct);

        return (effective & requiredFlags) == requiredFlags;
    }

    public async Task<Dictionary<string, long>> GetAllEffectiveAsync(
        Guid userId, Role role, CancellationToken ct = default)
    {
        var rolePerms = await _unitOfWork.Permissions.GetByRoleAsync(role, ct);
        var userOverrides = await _unitOfWork.Permissions.GetByUserIdAsync(userId, ct);

        var result = rolePerms.ToDictionary(rp => rp.Module, rp => rp.Flags);

        foreach (var uo in userOverrides)
        {
            if (result.TryGetValue(uo.Module, out var existing))
                result[uo.Module] = existing | uo.Flags;
            else
                result[uo.Module] = uo.Flags;
        }

        return result;
    }

    public async Task RequirePermissionAsync(
        Guid userId, Role role, string module, long requiredFlags,
        CancellationToken ct = default)
    {
        if (!await HasPermissionAsync(userId, role, module, requiredFlags, ct))
            throw new ForbiddenException(module, requiredFlags);
    }
}
