using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Permissions;

/// <summary>
/// Service kiểm tra quyền với hỗ trợ 3-tier hierarchical model:
/// Tier 1: RBAC (Role-Based) - subsystem + permissions
/// Tier 2: ABAC (Attribute-Based) - organization scope (region/company/department)
/// Tier 3: Entity-level (optional) - per-resource restrictions
/// </summary>
public interface IHierarchicalPermissionService
{
    /// <summary>
    /// Kiểm tra người dùng có quyền trên subsystem trong scope của họ không
    /// </summary>
    Task<bool> HasPermissionInUserScopeAsync(
        Guid userId,
        Guid subsystemId,
        Permission requiredPermission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra người dùng có quyền trên subsystem trong scope cụ thể không
    /// VD: User "ABC-Corp-Hanoi" có quyền trên subsystem ở scope "ABC-Corp-HCM"?
    /// </summary>
    Task<bool> HasPermissionInScopeAsync(
        Guid userId,
        Guid subsystemId,
        Permission requiredPermission,
        Guid? targetRegionId = null,
        Guid? targetCompanyId = null,
        Guid? targetDepartmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả scopes (region/company/department) mà user có access
    /// </summary>
    Task<List<OrganizationScope>> GetUserAccessibleScopesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch check quyền cho nhiều resources cùng lúc
    /// </summary>
    Task<Dictionary<string, bool>> CheckPermissionsAsync(
        Guid userId,
        Guid subsystemId,
        Permission requiredPermission,
        List<OrganizationScope> resourceScopes,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO đại diện một Organization Scope
/// </summary>
public class OrganizationScope
{
    public Guid? RegionId { get; set; }
    public string? RegionCode { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyCode { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentCode { get; set; }

    /// <summary>
    /// Scope ID dùng cho batch checking (VD: region-uuid hoặc company-uuid)
    /// </summary>
    public string GetScopeKey()
    {
        if (DepartmentId.HasValue) return $"dept-{DepartmentId}";
        if (CompanyId.HasValue) return $"co-{CompanyId}";
        if (RegionId.HasValue) return $"reg-{RegionId}";
        return "global";
    }

    /// <summary>
    /// Check xem scope này có match với target scope không
    /// null = wildcard (match any)
    /// </summary>
    public bool Matches(Guid? targetRegionId, Guid? targetCompanyId, Guid? targetDepartmentId)
    {
        // Department level - cái này specific nhất
        if (DepartmentId.HasValue && targetDepartmentId.HasValue)
            return DepartmentId == targetDepartmentId;

        // Company level
        if (CompanyId.HasValue && targetCompanyId.HasValue)
            return CompanyId == targetCompanyId;

        // Region level
        if (RegionId.HasValue && targetRegionId.HasValue)
            return RegionId == targetRegionId;

        // Global (wildcard) - nếu scope là global hoặc target là null
        if (!RegionId.HasValue && !targetRegionId.HasValue)
            return true;

        return false;
    }
}

/// <summary>
/// Implementation của IHierarchicalPermissionService
/// </summary>
public class HierarchicalPermissionService : IHierarchicalPermissionService
{
    private readonly AppDbContext _context;

    public HierarchicalPermissionService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Kiểm tra user có permission trong user's own scope
    /// </summary>
    public async Task<bool> HasPermissionInUserScopeAsync(
        Guid userId,
        Guid subsystemId,
        Permission requiredPermission,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RoleSubsystemPermissions)
            .ThenInclude(rsp => rsp.Subsystem)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.OrganizationScopes)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null || !user.UserRoles.Any())
            return false;

        // Check xem user's scopes có match với user's own org không
        return await HasPermissionInScopeAsync(
            userId,
            subsystemId,
            requiredPermission,
            user.RegionId,
            user.CompanyId,
            user.DepartmentId,
            cancellationToken);
    }

    /// <summary>
    /// Kiểm tra user có permission trong target scope
    /// </summary>
    public async Task<bool> HasPermissionInScopeAsync(
        Guid userId,
        Guid subsystemId,
        Permission requiredPermission,
        Guid? targetRegionId = null,
        Guid? targetCompanyId = null,
        Guid? targetDepartmentId = null,
        CancellationToken cancellationToken = default)
    {
        var userRoles = await _context.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.UserRoles)
            .Where(ur => ur.IsActive())
            .Include(ur => ur.Role)
            .ThenInclude(r => r.OrganizationScopes)
            .Include(ur => ur.Role)
            .ThenInclude(r => r.RoleSubsystemPermissions.Where(rsp => rsp.SubsystemId == subsystemId))
            .ToListAsync(cancellationToken);

        if (!userRoles.Any())
            return false;

        // Check từng role
        foreach (var userRole in userRoles)
        {
            var role = userRole.Role;

            // 1. Check RBAC: Role có permission trong subsystem này không?
            var subsystemPermission = role.RoleSubsystemPermissions
                .FirstOrDefault(rsp => rsp.SubsystemId == subsystemId);

            if (subsystemPermission == null)
                continue; // Role này không có access to subsystem

            // Check permission flags
            if ((subsystemPermission.Flags & (long)requiredPermission) == 0)
                continue; // Permission flag không match

            // 2. Check ABAC: Role's scope có cover target scope không?
            var scopes = role.OrganizationScopes.ToList();

            // Nếu role không có scope restrictions: global access (hỗ trợ superadmin)
            if (!scopes.Any())
                return true;

            // Check xem role's scope nào match với target scope
            foreach (var scope in scopes)
            {
                if (ScopeMatches(scope, targetRegionId, targetCompanyId, targetDepartmentId))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Lấy tất cả scopes mà user accessible
    /// </summary>
    public async Task<List<OrganizationScope>> GetUserAccessibleScopesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var roles = await _context.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.UserRoles)
            .Where(ur => ur.IsActive())
            .Include(ur => ur.Role)
            .ThenInclude(r => r.OrganizationScopes)
            .Select(ur => ur.Role)
            .ToListAsync(cancellationToken);

        var scopes = new List<OrganizationScope>();

        // Collect scopes từ tất cả roles
        foreach (var role in roles)
        {
            var activeScopes = role.OrganizationScopes.Where(s => s.IsActive).ToList();

            if (!activeScopes.Any())
            {
                // Global access - thêm special marker
                scopes.Add(new OrganizationScope { });
            }
            else
            {
                foreach (var scope in activeScopes)
                {
                    scopes.Add(new OrganizationScope
                    {
                        RegionId = scope.RegionId,
                        RegionCode = scope.Region?.Code,
                        CompanyId = scope.CompanyId,
                        CompanyCode = scope.Company?.Code,
                        DepartmentId = scope.DepartmentId,
                        DepartmentCode = scope.Department?.Code
                    });
                }
            }
        }

        // Remove duplicates
        return scopes.DistinctBy(s => s.GetScopeKey()).ToList();
    }

    /// <summary>
    /// Batch check permissions cho multiple resources
    /// </summary>
    public async Task<Dictionary<string, bool>> CheckPermissionsAsync(
        Guid userId,
        Guid subsystemId,
        Permission requiredPermission,
        List<OrganizationScope> resourceScopes,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, bool>();

        foreach (var resourceScope in resourceScopes)
        {
            var hasPermission = await HasPermissionInScopeAsync(
                userId,
                subsystemId,
                requiredPermission,
                resourceScope.RegionId,
                resourceScope.CompanyId,
                resourceScope.DepartmentId,
                cancellationToken);

            result[resourceScope.GetScopeKey()] = hasPermission;
        }

        return result;
    }

    // Helper methods

    /// <summary>
    /// Check xem role's scope có cover target scope không
    /// Scope hierarchy: Department > Company > Region > Global
    /// </summary>
    private static bool ScopeMatches(
        RoleOrganizationScope roleScope,
        Guid? targetRegionId,
        Guid? targetCompanyId,
        Guid? targetDepartmentId)
    {
        // Department level - most specific
        if (roleScope.DepartmentId.HasValue)
        {
            return roleScope.DepartmentId == targetDepartmentId ||
                   (targetDepartmentId == null && roleScope.CompanyId == targetCompanyId);
        }

        // Company level
        if (roleScope.CompanyId.HasValue)
        {
            return roleScope.CompanyId == targetCompanyId ||
                   (targetCompanyId == null && roleScope.RegionId == targetRegionId);
        }

        // Region level
        if (roleScope.RegionId.HasValue)
        {
            return roleScope.RegionId == targetRegionId || targetRegionId == null;
        }

        // Global scope (all nulls)
        return true;
    }
}
