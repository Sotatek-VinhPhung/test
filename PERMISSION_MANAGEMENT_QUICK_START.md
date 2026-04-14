# 🚀 **PERMISSION MANAGEMENT QUICK START**

## **Nhanh gọn - Những gì bạn cần làm**

### **1. Các Endpoints Có Sẵn**

```
✅ POST   /api/permissions/users/{userId}/roles
✅ DELETE /api/permissions/users/{userId}/roles/{roleId}
✅ PUT    /api/permissions/roles/{roleId}/subsystems/{subsystemId}/permissions
✅ POST   /api/permissions/users/{userId}/subsystems/{subsystemId}/permissions/override
✅ DELETE /api/permissions/users/{userId}/subsystems/{subsystemId}/permissions/override
✅ GET    /api/permissions/roles/{roleId}/users
```

### **2. Điều Kiện Tiên Quyết**

```json
Bạn cần JWT Token có chứa:
{
  "Permission:Settings": "12288"  // = 4096 (ManageRoles) | 8192 (ManagePermissions)
}
```

### **3. Những Lệnh Thường Dùng**

#### **Gán Role**
```bash
POST /api/permissions/users/{userId}/roles
{
  "roleId": "guid"
}
```

#### **Xóa Role**
```bash
DELETE /api/permissions/users/{userId}/roles/{roleId}
```

#### **Cập Nhật Quyền Role**
```bash
PUT /api/permissions/roles/{roleId}/subsystems/{subsystemId}/permissions
{
  "permissionNames": ["View", "Create", "Edit"]
}
```

#### **Override Quyền User**
```bash
POST /api/permissions/users/{userId}/subsystems/{subsystemId}/permissions/override
{
  "permissionNames": ["View", "Export", "Approve"],
  "reason": "Special access for project X"
}
```

### **4. Architecture**

```
Request with [RequirePermission] attribute
        ↓
PermissionsController endpoint
        ↓
IRoleManagementService (đã inject)
        ↓
AppDbContext (CRUD operations)
        ↓
Update database + Invalidate cache
        ↓
IUserContextService.InvalidateUserContextAsync()
        ↓
Next request → Fresh permissions từ DB
```

### **5. Key Components Added**

| File | Mục Đích |
|------|---------|
| `RoleManagementService.cs` | Implementation quản lý quyền |
| `IRoleManagementService.cs` | Interface dịch vụ |
| DTOs | Assign, Revoke, Update requests/responses |
| `PermissionsController.cs` | Endpoints (updated with new actions) |
| `DependencyInjection.cs` | Service registration (added) |

### **6. Quy Trình Cơ Bản**

**Gán role:**
```csharp
var result = await _roleManagementService.AssignRoleToUserAsync(userId, roleId);
// → RoleAssignmentResponse với status "Assigned"
```

**Sửa quyền:**
```csharp
var result = await _roleManagementService.UpdateRolePermissionsAsync(roleId, subsystemId, flags);
// → PermissionUpdateResponse với permission names
```

### **7. Test Cách Nhanh Nhất**

1. Lấy Admin token
2. Gọi: `POST /api/permissions/users/{some-user}/roles`  
3. Body: `{ "roleId": "{viewer-role-guid}" }`
4. Xong! ✅

---

**Hết rồi. Build successful. Ready to use!** 🎉
