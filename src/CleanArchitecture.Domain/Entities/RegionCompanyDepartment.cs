using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities;

/// <summary>
/// Đại diện một khu vực địa lý (VD: Hà Nội, HCM, Singapore)
/// </summary>
public class Region : BaseEntity
{
    public string Code { get; set; } = ""; // VN-HN, VN-HCM, SG, etc.
    public string Name { get; set; } = ""; // Hanoi, Ho Chi Minh, Singapore
    public string Country { get; set; } = ""; // Vietnam, Singapore, etc.

    // Navigation
    public ICollection<Company> Companies { get; set; } = new List<Company>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<RoleOrganizationScope> RoleScopes { get; set; } = new List<RoleOrganizationScope>();
}

/// <summary>
/// Đại diện công ty (có thể span nhiều khu vực)
/// </summary>
public class Company : BaseEntity
{
    public string Code { get; set; } = ""; // ABC-CORP, XYZ-TECH, etc.
    public string Name { get; set; } = ""; // ABC Corporation, XYZ Technology
    public string TaxId { get; set; } = ""; // MST/TaxID
    public Guid? RegionId { get; set; } // Nullable - công ty chính ở toàn bộ khu vực hoặc một khu vực cụ thể

    // Navigation
    public Region? Region { get; set; }
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<RoleOrganizationScope> RoleScopes { get; set; } = new List<RoleOrganizationScope>();
}

/// <summary>
/// Đại diện phòng ban trong công ty
/// </summary>
public class Department : BaseEntity
{
    public string Code { get; set; } = ""; // ACC-ACCOUNTING, HR-HR, IT-SUPPORT, etc.
    public string Name { get; set; } = ""; // Accounting, HR, IT Support
    public Guid CompanyId { get; set; } // Required - phòng ban luôn thuộc một công ty

    // Navigation
    public Company Company { get; set; } = null!;
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<RoleOrganizationScope> RoleScopes { get; set; } = new List<RoleOrganizationScope>();
}

/// <summary>
/// Giới hạn phạm vi một role (ABAC - Attribute-Based Access Control)
/// 
/// VD: Role "Kế toán trưởng" có thể restricted:
/// - Chỉ ở Region "VN-HN"
/// - Chỉ ở Company "ABC-CORP"
/// - Chỉ ở Department "ACC-ACCOUNTING"
/// - Hoặc kết hợp bất kỳ level nào
/// 
/// Nếu tất cả null: Role có quyền toàn cục (superadmin)
/// </summary>
public class RoleOrganizationScope : BaseEntity
{
    public Guid RoleId { get; set; } // Required - phải assign cho một role
    public Guid? RegionId { get; set; } // Nullable - null = tất cả regions
    public Guid? CompanyId { get; set; } // Nullable - null = tất cả companies
    public Guid? DepartmentId { get; set; } // Nullable - null = tất cả departments

    // Navigation
    public Role Role { get; set; } = null!;
    public Region? Region { get; set; }
    public Company? Company { get; set; }
    public Department? Department { get; set; }
}
