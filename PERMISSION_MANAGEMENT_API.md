# 🛠️ **API QUẢN LÝ QUYỀN - HƯỚNG DẪN SỬ DỤNG**

## 📌 **Tổng Quan**

Hệ thống RBAC đã được bổ sung đầy đủ các endpoints để **quản lý quyền** của user:

- ✅ Gán role cho user
- ✅ Xóa role khỏi user
- ✅ Sửa quyền của role
- ✅ Override quyền cho user cụ thể
- ✅ Xem danh sách users có role nào

---

## 🔐 **Authorization Requirements**

Tất cả endpoints quản lý quyền yêu cầu:

| Endpoint | Quyền Cần | PermissionModule |
|----------|-----------|-----------------|
| Assign Role | ManageRoles | Settings |
| Revoke Role | ManageRoles | Settings |
| Update Role Permissions | ManagePermissions | Settings |
| Override User Permissions | ManagePermissions | Settings |
| Get Users with Role | ManageRoles | Settings |

**JWT Token phải chứa permission claim:**

```json
{
  "sub": "user-id",
  "email": "admin@example.com",
  "role": "Admin",
  "Permission:Settings": "12288"  // ← ManageRoles (4096) | ManagePermissions (8192)
}
```

---

## 🚀 **API Endpoints**

### **1️⃣ Gán Role Cho User**

**POST** `/api/permissions/users/{userId}/roles`

```bash
curl -X POST "https://localhost:5001/api/permissions/users/user-guid-123/roles" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "roleId": "role-guid-456"
  }'
```

**Request Body:**

```json
{
  "userId": "00000000-0000-0000-0000-000000000001",
  "roleId": "10000000-0000-0000-0000-000000000001"
}
```

**Response (200 OK):**

```json
{
  "userId": "00000000-0000-0000-0000-000000000001",
  "roleId": "10000000-0000-0000-0000-000000000001",
  "roleCode": "Admin",
  "operation": "Assigned",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Error Responses:**

- `400 Bad Request`: User or role not found, or user already has role
- `403 Forbidden`: No ManageRoles permission

---

### **2️⃣ Xóa Role Khỏi User**

**DELETE** `/api/permissions/users/{userId}/roles/{roleId}`

```bash
curl -X DELETE "https://localhost:5001/api/permissions/users/user-guid/roles/role-guid" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response (200 OK):**

```json
{
  "userId": "00000000-0000-0000-0000-000000000001",
  "roleId": "10000000-0000-0000-0000-000000000001",
  "roleCode": "Admin",
  "operation": "Revoked",
  "createdAt": "2024-01-15T10:31:00Z"
}
```

---

### **3️⃣ Sửa Quyền Của Role**

**PUT** `/api/permissions/roles/{roleId}/subsystems/{subsystemId}/permissions`

Có 2 cách để chỉ định quyền:

#### **Cách 1: Dùng Flags (Bitwise)**

```bash
curl -X PUT "https://localhost:5001/api/permissions/roles/role-guid/subsystems/subsys-guid/permissions" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "flags": 7
  }'
```

**Request Body:**

```json
{
  "roleId": "10000000-0000-0000-0000-000000000002",
  "subsystemId": "00000000-0000-0000-0000-000000000002",
  "flags": 7
}
```

**Giải thích flags = 7:**
- Binary: `111`
- = View(1) + Create(2) + Edit(4)

#### **Cách 2: Dùng Permission Names (Dễ hơn)**

```bash
curl -X PUT "https://localhost:5001/api/permissions/roles/role-guid/subsystems/subsys-guid/permissions" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "permissionNames": ["View", "Create", "Edit", "Delete"]
  }'
```

**Request Body:**

```json
{
  "roleId": "10000000-0000-0000-0000-000000000002",
  "subsystemId": "00000000-0000-0000-0000-000000000002",
  "permissionNames": ["View", "Create", "Edit", "Delete"]
}
```

**Response (200 OK):**

```json
{
  "entityId": "10000000-0000-0000-0000-000000000002",
  "subsystemId": "00000000-0000-0000-0000-000000000002",
  "subsystemCode": "Users",
  "flags": 15,
  "permissionNames": ["View", "Create", "Edit", "Delete"],
  "updatedAt": "2024-01-15T10:32:00Z"
}
```

---

### **4️⃣ Override Quyền Cho User Cụ Thể**

**POST** `/api/permissions/users/{userId}/subsystems/{subsystemId}/permissions/override`

Override giúp cấp quyền **riêng** cho user, không phụ thuộc role.

#### **Cách 1: Dùng Flags**

```bash
curl -X POST "https://localhost:5001/api/permissions/users/user-guid/subsystems/subsys-guid/permissions/override" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "flags": 256,
    "reason": "Special permission for report generation"
  }'
```

**Request Body:**

```json
{
  "userId": "00000000-0000-0000-0000-000000000001",
  "subsystemId": "00000000-0000-0000-0000-000000000002",
  "flags": 256,
  "reason": "Temporary elevated access for audit"
}
```

#### **Cách 2: Dùng Permission Names**

```json
{
  "userId": "00000000-0000-0000-0000-000000000001",
  "subsystemId": "00000000-0000-0000-0000-000000000002",
  "permissionNames": ["View", "Edit", "Export", "Approve"],
  "reason": "Special permissions for this campaign"
}
```

**Response (200 OK):**

```json
{
  "entityId": "00000000-0000-0000-0000-000000000001",
  "subsystemId": "00000000-0000-0000-0000-000000000002",
  "subsystemCode": "Reports",
  "flags": 256,
  "permissionNames": ["ManageUsers"],
  "updatedAt": "2024-01-15T10:33:00Z"
}
```

---

### **5️⃣ Xóa Override Quyền**

**DELETE** `/api/permissions/users/{userId}/subsystems/{subsystemId}/permissions/override`

```bash
curl -X DELETE "https://localhost:5001/api/permissions/users/user-guid/subsystems/subsys-guid/permissions/override" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response (204 No Content):**

```
(Empty body, just status 204)
```

---

### **6️⃣ Xem Danh Sách Users Có Role**

**GET** `/api/permissions/roles/{roleId}/users`

```bash
curl -X GET "https://localhost:5001/api/permissions/roles/role-guid/users" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response (200 OK):**

```json
[
  "00000000-0000-0000-0000-000000000001",
  "00000000-0000-0000-0000-000000000002",
  "00000000-0000-0000-0000-000000000003"
]
```

---

## 📊 **Permission Flags Cheat Sheet**

| Permission | Bit | Value | Dùng Cho |
|-----------|-----|-------|----------|
| View | 0 | 1 | Xem dữ liệu |
| Create | 1 | 2 | Tạo dữ liệu mới |
| Edit | 2 | 4 | Sửa dữ liệu |
| Delete | 3 | 8 | Xóa dữ liệu |
| Export | 4 | 16 | Xuất dữ liệu |
| Approve | 5 | 32 | Phê duyệt |
| Execute | 6 | 64 | Chạy lệnh/report |
| Audit | 7 | 128 | Xem audit logs |
| ManageUsers | 8 | 256 | Quản lý users |
| ViewReports | 9 | 512 | Xem reports module |
| EditReports | 10 | 1024 | Sửa reports |
| ScheduleReports | 11 | 2048 | Lập lịch reports |
| ManageRoles | 12 | 4096 | Quản lý roles |
| ManagePermissions | 13 | 8192 | Quản lý quyền |

### **Ví Dụ Bitwise Combining:**

```
Admin role: View(1) + Create(2) + Edit(4) + Delete(8) + Approve(32) + ManageRoles(4096)
          = 1 | 2 | 4 | 8 | 32 | 4096
          = 4143

Manager role: View(1) + Create(2) + Edit(4) + Approve(32)
            = 1 | 2 | 4 | 32
            = 39

Viewer role: View(1)
           = 1
```

---

## 🔄 **Permission Flow After Updates**

```
1. Admin gán role cho user → _userContextService.InvalidateUserContextAsync()
   ↓
2. Cache cleared → Next request sẽ load permissions từ DB
   ↓
3. UserContextService rebuild context → Merge quyền từ roles + overrides
   ↓
4. JwtTokenGenerator tạo token mới với permission claims
   ↓
5. PermissionAuthorizationHandler validate bitwise flags
   ↓
6. Action được phép chạy hoặc 403 Forbidden
```

---

## ✅ **Các Trường Hợp Sử Dụng**

### **1. Cấp Admin cho user mới**

```bash
# Gán Admin role
curl -X POST "http://localhost:5001/api/permissions/users/new-user-id/roles" \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -d '{ "roleId": "admin-role-id" }' \
  -H "Content-Type: application/json"
```

### **2. Tạm cấp quyền cho dự án cụ thể**

```bash
# Override quyền Export cho user trên Reports subsystem
curl -X POST "http://localhost:5001/api/permissions/users/user-id/subsystems/reports-subsys-id/permissions/override" \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -d '{
    "permissionNames": ["View", "Edit", "Export"],
    "reason": "Q1 2024 campaign - temporary"
  }' \
  -H "Content-Type: application/json"
```

### **3. Nâng role từ Viewer → Manager**

```bash
# 1. Xóa Viewer role
curl -X DELETE "http://localhost:5001/api/permissions/users/user-id/roles/viewer-role-id" \
  -H "Authorization: Bearer ADMIN_TOKEN"

# 2. Gán Manager role
curl -X POST "http://localhost:5001/api/permissions/users/user-id/roles" \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -d '{ "roleId": "manager-role-id" }' \
  -H "Content-Type: application/json"
```

### **4. Sửa quyền của role**

```bash
# Manager không được xóa → Xóa Delete permission
curl -X PUT "http://localhost:5001/api/permissions/roles/manager-role-id/subsystems/users-subsys-id/permissions" \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -d '{
    "permissionNames": ["View", "Create", "Edit", "Export"]
  }' \
  -H "Content-Type: application/json"
```

---

## 🧪 **Testing**

### **Postman Collection**

```
1. Set Variables:
   - baseUrl: http://localhost:5001
   - adminToken: YOUR_ADMIN_JWT
   - userId: some-user-guid
   - roleId: some-role-guid
   - subsystemId: some-subsys-guid

2. Assign Role
   POST {{baseUrl}}/api/permissions/users/{{userId}}/roles
   Body: { "roleId": "{{roleId}}" }

3. Update Role Permissions
   PUT {{baseUrl}}/api/permissions/roles/{{roleId}}/subsystems/{{subsystemId}}/permissions
   Body: { "permissionNames": ["View", "Create", "Edit"] }

4. Get Users with Role
   GET {{baseUrl}}/api/permissions/roles/{{roleId}}/users
```

---

## ⚠️ **Lưu Ý Quan Trọng**

1. **Cache Invalidation**: Sau mỗi thay đổi quyền, user context cache tự động cleared
2. **Immediate Effect**: User cần re-login hoặc request JWT token mới để nhận quyền mới
3. **Atomic Operations**: Mỗi endpoint là atomic - hoặc thành công hoặc rollback hoàn toàn
4. **Audit Trail**: Nên log tất cả permission changes (override có `Reason` field sẵn)
5. **Performance**: Bitwise operations là O(1), rất nhanh ngay cả với nhiều roles

---

## 📚 **Xem Thêm**

- [RBAC Architecture Overview](RBAC_SUMMARY.md)
- [Permission Bitwise Operations](PERMISSION_MULTIPLE_FLAGS_IMPLEMENTATION.md)
- [Database Setup](database/setup_rbac_system_v2.sql)
