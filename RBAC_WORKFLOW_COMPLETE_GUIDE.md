# 🔐 Complete RBAC Workflow Guide - "Kế toán trưởng" Example

## 📊 Overall Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│ PHASE 1: SETUP (Admin Only - One-time or when roles change)        │
└─────────────────────────────────────────────────────────────────────┘

STEP 1: Create Role + Assign Permissions to Subsystem
   Admin → AdminSetupController.SetupRole()
   ├─ Role Code: "ChiefAccountant"
   ├─ Role Name: "Kế toán trưởng"
   ├─ Subsystems: ["ReportsAccounting"]
   └─ Permissions: ["View", "Create", "Edit", "Delete", "Export", "Approve", "Execute"]
   
   Database Result:
   ├─ roles table: INSERT (id=UUID, code='ChiefAccountant', name='Kế toán trưởng')
   └─ role_subsystem_permissions table: INSERT (roleId=UUID, subsystemId=UUID, flags=0x7F)

┌─────────────────────────────────────────────────────────────────────┐
│ PHASE 2: ASSIGN (Admin assigns role to specific user)              │
└─────────────────────────────────────────────────────────────────────┘

STEP 2: Assign Role to User
   Admin → AdminSetupController.AssignRoleToUser(userId="user123")
   ├─ Role Code: "ChiefAccountant"
   
   Database Result:
   └─ user_roles table: INSERT (userId='user123', roleId=UUID)

┌─────────────────────────────────────────────────────────────────────┐
│ PHASE 3: AUTHORIZATION (Runtime - Every API request)               │
└─────────────────────────────────────────────────────────────────────┘

STEP 3: User Makes Request to Protected Endpoint
   User (user123) → GET /api/reports/accounting
   
   Endpoint has: [RequirePermission("ReportsAccounting", (long)Permission.View)]
   
   ┌─ Authorization Handler Executes:
   │  1. Extract user ID from JWT: "sub" claim = "user123"
   │  2. Get subsystem: WHERE code='ReportsAccounting' → subsystemId=UUID
   │  3. Query user roles: WHERE userId='user123' → roleIds=[UUID]
   │  4. Check permission:
   │     SELECT flags FROM role_subsystem_permissions
   │     WHERE roleId IN (roleIds) AND subsystemId=UUID
   │     5. Bitwise check: (flags & Permission.View) == Permission.View?
   │        → YES: User allowed ✅
   │        → NO: Unauthorized 403 ❌
   │
   └─ Request proceeds to controller action
```

---

## 🎯 Step-by-Step Walkthrough: "Kế toán trưởng" Example

### ✅ STEP 1: Setup Role with Full Permissions (Admin Action)

**Request:**
```http
POST /api/admin/setup-role
Content-Type: application/json
Authorization: Bearer <admin-token>

{
  "roleCode": "ChiefAccountant",
  "roleName": "Kế toán trưởng",
  "description": "Chief Accountant - Full access to accounting reports",
  "subsystemCodes": ["ReportsAccounting"],
  "permissions": [
    "View",
    "Create", 
    "Edit",
    "Delete",
    "Export",
    "Approve",
    "Execute"
  ]
}
```

**What Happens in Database:**

```sql
-- Check if role exists
SELECT * FROM roles WHERE code = 'ChiefAccountant';
-- Result: NOT FOUND (first time), so create it

-- INSERT role
INSERT INTO roles (id, code, name, description, is_active, created_at)
VALUES ('550e8400-e29b-41d4-a716-446655440000', 'ChiefAccountant', 'Kế toán trưởng', '...', true, NOW());

-- Get subsystem ID
SELECT id FROM subsystems WHERE code = 'ReportsAccounting';
-- Result: '660e8400-e29b-41d4-a716-446655440000'

-- Convert permissions to flags
-- View=1, Create=2, Edit=4, Delete=8, Export=16, Approve=32, Execute=64
-- Combined flags = 1 + 2 + 4 + 8 + 16 + 32 + 64 = 127 (0x7F in hex)

-- Assign permissions
INSERT INTO role_subsystem_permissions 
  (role_id, subsystem_id, flags, created_at)
VALUES 
  ('550e8400-e29b-41d4-a716-446655440000', 
   '660e8400-e29b-41d4-a716-446655440000', 
   127, 
   NOW());
```

**Response:**
```json
{
  "success": true,
  "roleId": "550e8400-e29b-41d4-a716-446655440000",
  "roleName": "Kế toán trưởng",
  "message": "Role created and permissions assigned successfully"
}
```

**Database State After Step 1:**
```
TABLE: roles
┌──────────────────────────────────┬────────────────────┬──────────────────┐
│ id                               │ code               │ name             │
├──────────────────────────────────┼────────────────────┼──────────────────┤
│ 550e8400-e29b-41d4-a716-4466... │ ChiefAccountant    │ Kế toán trưởng    │
└──────────────────────────────────┴────────────────────┴──────────────────┘

TABLE: role_subsystem_permissions
┌──────────────────────────────────┬──────────────────────────────────┬───────┐
│ role_id                          │ subsystem_id                     │ flags │
├──────────────────────────────────┼──────────────────────────────────┼───────┤
│ 550e8400-e29b-41d4-a716-4466... │ 660e8400-e29b-41d4-a716-4466... │ 127   │
└──────────────────────────────────┴──────────────────────────────────┴───────┘
```

---

### ✅ STEP 2: Create User (User Management)

**Request:**
```http
POST /api/users
Content-Type: application/json
Authorization: Bearer <admin-token>

{
  "email": "tran.duong@company.com",
  "firstName": "Trần",
  "lastName": "Dương",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "id": "770e8400-e29b-41d4-a716-446655440000",
  "email": "tran.duong@company.com",
  "firstName": "Trần",
  "lastName": "Dương"
}
```

**Database State:**
```
TABLE: users
┌──────────────────────────────────┬──────────────────────────┐
│ id                               │ email                    │
├──────────────────────────────────┼──────────────────────────┤
│ 770e8400-e29b-41d4-a716-4466... │ tran.duong@company.com   │
└──────────────────────────────────┴──────────────────────────┘
```

---

### ✅ STEP 3: Assign Role to User (Admin Action)

**Request:**
```http
POST /api/admin/users/770e8400-e29b-41d4-a716-446655440000/assign-role
Content-Type: application/json
Authorization: Bearer <admin-token>

{
  "roleCode": "ChiefAccountant"
}
```

**What Happens in Database:**

```sql
-- Get role ID by code
SELECT id FROM roles WHERE code = 'ChiefAccountant';
-- Result: '550e8400-e29b-41d4-a716-446655440000'

-- Create user-role relationship
INSERT INTO user_roles (user_id, role_id, created_at)
VALUES 
  ('770e8400-e29b-41d4-a716-446655440000',  -- user.id
   '550e8400-e29b-41d4-a716-446655440000',  -- role.id (ChiefAccountant)
   NOW());
```

**Response:**
```json
{
  "success": true,
  "userId": "770e8400-e29b-41d4-a716-446655440000",
  "roleCode": "ChiefAccountant",
  "message": "Role assigned successfully"
}
```

**Database State After Step 3:**
```
TABLE: user_roles
┌──────────────────────────────────┬──────────────────────────────────┐
│ user_id                          │ role_id                          │
├──────────────────────────────────┼──────────────────────────────────┤
│ 770e8400-e29b-41d4-a716-4466... │ 550e8400-e29b-41d4-a716-4466... │
│                                  │ (ChiefAccountant)                │
└──────────────────────────────────┴──────────────────────────────────┘
```

---

### ✅ STEP 4: Verify User's Effective Permissions (Debug)

**Request:**
```http
GET /api/admin/users/770e8400-e29b-41d4-a716-446655440000/effective-permissions
Authorization: Bearer <admin-token>
```

**What Happens:**

```sql
-- Get all roles assigned to user
SELECT role_id FROM user_roles 
WHERE user_id = '770e8400-e29b-41d4-a716-446655440000';
-- Result: ['550e8400-e29b-41d4-a716-446655440000']

-- Get all permissions for each role across all subsystems
SELECT 
  s.code AS subsystem_code,
  rsp.flags
FROM role_subsystem_permissions rsp
JOIN subsystems s ON rsp.subsystem_id = s.id
WHERE rsp.role_id IN ('550e8400-e29b-41d4-a716-446655440000');

-- Result:
-- subsystem_code='ReportsAccounting', flags=127
```

**Response:**
```json
{
  "userId": "770e8400-e29b-41d4-a716-446655440000",
  "effectivePermissions": {
    "ReportsAccounting": {
      "view": true,
      "create": true,
      "edit": true,
      "delete": true,
      "export": true,
      "approve": true,
      "execute": true,
      "audit": false,
      "manageUsers": false,
      "viewReports": false,
      "editReports": false,
      "scheduleReports": false,
      "manageRoles": false,
      "managePermissions": false
    }
  }
}
```

---

### ✅ STEP 5: User Accesses Protected Endpoint

**Request (from user client):**
```http
GET /api/reports/accounting
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
  (Token contains claims: sub="770e8400-e29b-41d4-a716-446655440000")
```

**Endpoint Definition:**
```csharp
[HttpGet]
[Authorize]
[RequirePermission("ReportsAccounting", (long)Permission.View)]
public async Task<IActionResult> GetAccountingReports()
{
    return Ok(new { data = "Accounting Reports Data" });
}
```

**Authorization Flow (PermissionAuthorizationHandler):**

```
1. Extract User ID from JWT
   ├─ Look for claim: "sub"
   └─ Found: "770e8400-e29b-41d4-a716-446655440000" ✅

2. Get Subsystem ID
   ├─ Query: WHERE code = 'ReportsAccounting'
   └─ Found: subsystemId = "660e8400-e29b-41d4-a716-446655440000" ✅

3. Get User's Roles
   ├─ Query: SELECT role_id FROM user_roles 
   │         WHERE user_id = '770e8400-e29b-41d4-a716-446655440000'
   └─ Found: roleIds = ["550e8400-e29b-41d4-a716-446655440000"] ✅

4. Check Permission (Bitwise AND)
   ├─ Query: SELECT flags FROM role_subsystem_permissions
   │         WHERE role_id IN (["550e8400-e29b-41d4-a716-446655440000"]) 
   │         AND subsystem_id = "660e8400-e29b-41d4-a716-446655440000"
   ├─ Found: flags = 127 (binary: 01111111)
   ├─ Check: (127 & Permission.View) == Permission.View?
   │        (127 & 1) == 1?
   │        1 == 1? YES ✅
   └─ Result: ALLOWED

5. Execute Endpoint
   ├─ Return: 200 OK with response
   └─ User sees: Accounting Reports Data ✅
```

**Response (Success):**
```json
{
  "data": "Accounting Reports Data"
}
```

**Status Code:** `200 OK` ✅

---

## 🔄 What If User Doesn't Have Permission?

### Scenario: User Tries to Delete (but role only has View + Create + Edit)

**Same endpoint but with Delete permission:**
```csharp
[HttpDelete("reports/{id}")]
[RequirePermission("ReportsAccounting", (long)Permission.Delete)]
public async Task<IActionResult> DeleteReport(Guid id)
{
    return Ok(new { message = "Report deleted" });
}
```

**Authorization Check:**
```
1. User ID: "770e8400-e29b-41d4-a716-446655440000" ✅
2. Subsystem: "ReportsAccounting" ✅
3. User Roles: ["ChiefAccountant"] ✅
4. Role Permissions: flags = 127

   Wait... 127 includes DELETE!
   Binary: 01111111
   Bit 3 (Delete = 8): SET ✅
   
   So user CAN delete! ✅
   
   If role DIDN'T have delete:
   flags = 119 (View+Create+Edit+Export+Approve+Execute = 1+2+4+16+32+64)
   Binary: 01110111
   Bit 3 (Delete = 8): NOT SET
   
   Check: (119 & Permission.Delete) == Permission.Delete?
         (119 & 8) == 8?
         0 == 8? NO ❌
   
   Result: 403 FORBIDDEN
```

**Response (If no permission):**
```json
{
  "statusCode": 403,
  "message": "Unauthorized",
  "details": "User does not have required permission: Delete in subsystem ReportsAccounting"
}
```

**Status Code:** `403 Forbidden` ❌

---

## 📋 Permission Flags Explained

### Bitwise Flag Values

```
Permission.View          = 1       (0x01, binary: 00000001)
Permission.Create        = 2       (0x02, binary: 00000010)
Permission.Edit          = 4       (0x04, binary: 00000100)
Permission.Delete        = 8       (0x08, binary: 00001000)
Permission.Export        = 16      (0x10, binary: 00010000)
Permission.Approve       = 32      (0x20, binary: 00100000)
Permission.Execute       = 64      (0x40, binary: 01000000)
Permission.Audit         = 128     (0x80, binary: 10000000)
Permission.ManageUsers   = 256     (0x100, binary: 0000000100000000)
Permission.ManageRoles   = 512     (0x200, binary: 0000001000000000)
Permission.ManagePermissions = 1024 (0x400, binary: 0000010000000000)
```

### Combining Permissions

```csharp
// Single permission
long flags = (long)Permission.View;  // = 1

// Multiple permissions (bitwise OR)
long flags = (long)Permission.View 
           | (long)Permission.Create
           | (long)Permission.Edit;
// = 1 | 2 | 4 = 7 (binary: 00000111)

// Check if has permission (bitwise AND)
bool hasView = (flags & (long)Permission.View) == (long)Permission.View;
// (7 & 1) == 1? YES ✅

bool hasDelete = (flags & (long)Permission.Delete) == (long)Permission.Delete;
// (7 & 8) == 8? NO ❌
```

---

## 🔧 Complete API Workflow Examples

### Example 1: Chief Accountant Setup (Complete)

```bash
#!/bin/bash
API="http://localhost:5000"
ADMIN_TOKEN="<your-admin-jwt-token>"

# 1. Setup Role with Permissions
echo "=== STEP 1: Setup ChiefAccountant Role ==="
curl -X POST "$API/api/admin/setup-role" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "roleCode": "ChiefAccountant",
    "roleName": "Kế toán trưởng",
    "description": "Chief Accountant - Full access to accounting subsystem",
    "subsystemCodes": ["ReportsAccounting"],
    "permissions": ["View", "Create", "Edit", "Delete", "Export", "Approve", "Execute"]
  }'

# 2. Create Test User
echo "=== STEP 2: Create User ==="
curl -X POST "$API/api/users" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "tran.duong@company.com",
    "firstName": "Trần",
    "lastName": "Dương",
    "password": "SecurePassword123!"
  }'

USER_ID="<response.id>"  # Save from response

# 3. Assign Role to User
echo "=== STEP 3: Assign Role to User ==="
curl -X POST "$API/api/admin/users/$USER_ID/assign-role" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "roleCode": "ChiefAccountant"
  }'

# 4. Check User's Effective Permissions
echo "=== STEP 4: Verify Permissions ==="
curl -X GET "$API/api/admin/users/$USER_ID/effective-permissions" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# 5. Get JWT Token for User
echo "=== STEP 5: Login as User ==="
curl -X POST "$API/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "tran.duong@company.com",
    "password": "SecurePassword123!"
  }'

USER_TOKEN="<response.token>"  # Save from response

# 6. Access Protected Endpoint
echo "=== STEP 6: Access Protected Endpoint ==="
curl -X GET "$API/api/reports/accounting" \
  -H "Authorization: Bearer $USER_TOKEN"
  
# Expected Response: 200 OK with data ✅
```

---

## 📊 Database Query Reference

### Check User's Permissions

```sql
-- All subsystems user has access to with combined flags
SELECT 
    u.id as user_id,
    u.email,
    r.code as role_code,
    r.name as role_name,
    s.code as subsystem_code,
    s.name as subsystem_name,
    rsp.flags as permission_flags
FROM users u
JOIN user_roles ur ON u.id = ur.user_id
JOIN roles r ON ur.role_id = r.id
JOIN role_subsystem_permissions rsp ON r.id = rsp.role_id
JOIN subsystems s ON rsp.subsystem_id = s.id
WHERE u.id = '770e8400-e29b-41d4-a716-446655440000'
ORDER BY s.code, r.code;

-- Result for Chief Accountant user:
/*
user_id                               | email                      | role_code      | role_name      | subsystem_code    | subsystem_name     | permission_flags
770e8400-e29b-41d4-a716-446655440000 | tran.duong@company.com    | ChiefAccountant | Kế toán trưởng | ReportsAccounting | Accounting Reports | 127
*/
```

### Check if User Has Specific Permission

```sql
-- Does user have "View" (flag=1) on ReportsAccounting?
SELECT (rsp.flags & 1) = 1 as has_view_permission
FROM users u
JOIN user_roles ur ON u.id = ur.user_id
JOIN roles r ON ur.role_id = r.id
JOIN role_subsystem_permissions rsp ON r.id = rsp.role_id
JOIN subsystems s ON rsp.subsystem_id = s.id
WHERE u.id = '770e8400-e29b-41d4-a716-446655440000'
  AND s.code = 'ReportsAccounting'
  AND (rsp.flags & 1) = 1;

-- Result: has_view_permission = TRUE ✅
```

### Find All Users with ChiefAccountant Role

```sql
SELECT 
    u.id,
    u.email,
    u.first_name,
    u.last_name,
    r.name as role_name
FROM users u
JOIN user_roles ur ON u.id = ur.user_id
JOIN roles r ON ur.role_id = r.id
WHERE r.code = 'ChiefAccountant'
ORDER BY u.email;
```

---

## 🎓 Key Concepts

### 1. **Role** = Collection of Permissions
- ChiefAccountant role = View + Create + Edit + Delete + Export + Approve + Execute (on ReportsAccounting)

### 2. **Subsystem** = Feature/Module Area
- ReportsAccounting subsystem = Accounting reports feature area

### 3. **User → Role → Permissions Flow**
```
User (Trần Dương)
    ↓ has role
    ↓
ChiefAccountant Role
    ↓ assigned to subsystem
    ↓
ReportsAccounting Subsystem
    ↓ with permissions (flags=127)
    ↓
Can: View, Create, Edit, Delete, Export, Approve, Execute ✅
Cannot: Audit, ManageUsers, ManageRoles, ManagePermissions ❌
```

### 4. **Permission Check = Bitwise AND**
```
User.Flags = 127 (binary: 01111111 = all permissions)
Required = 1    (binary: 00000001 = View only)

Check: (127 & 1) == 1?
       (01111111 & 00000001) = 00000001?
       00000001 == 00000001?
       YES → Permission granted ✅
```

### 5. **Real-time Authorization**
- No JWT claim staleness issues
- Database is always source of truth
- Permission changes take effect immediately
- When admin changes role permissions → all users with that role get new permissions right away

---

## 🚀 Next Steps to Test

1. **Start the application:**
   ```bash
   dotnet run --project "src/CleanArchitecture.Api"
   ```

2. **Wait for seeding:** Check PostgreSQL to verify roles/subsystems populated

3. **Get admin token:** Login with admin credentials

4. **Run STEP 1-6** from the bash example above

5. **Verify database:** Check `user_roles` and `role_subsystem_permissions` tables

6. **Try endpoint:** Call a protected endpoint and confirm 200 OK response

---

## 📞 Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| "subsystem not found" | Seeder didn't run | Check Program.cs has `await app.SeedRbacAsync()` |
| "role not found" | Role wasn't created first | Call SetupRole before AssignRoleToUser |
| "403 Forbidden" on endpoint | User doesn't have permission | Check effective-permissions endpoint, then update role |
| "user not found" | Wrong user ID format | Verify it's a valid GUID |
| JWT token expired | Token validity window passed | Login again to get new token |

