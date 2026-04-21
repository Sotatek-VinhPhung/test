# ⚡ QUICK START: FROM ZERO TO RUNNING IN 5 MINUTES

## 📋 Checklist Trước Khi Bắt Đầu

- ✅ Visual Studio 2026 hoặc VS Code mở
- ✅ PostgreSQL Docker container sẵn sàng (hoặc PostgreSQL installed locally)
- ✅ .NET SDK 8.0+
- ⏱️ 5 phút trống rỗi

---

## 🚀 Let's Go!

### **Minute 1-2: Start Database**

```powershell
# Terminal 1 - Start PostgreSQL
cd C:\test
docker-compose -f docker-compose.yml up -d

# Verify it's running
docker ps | grep postgres
# Expected: Should see postgres container running
```

**If Docker fails:**
```powershell
# Make sure Docker Desktop is running
# Windows: Start Docker Desktop app first
```

---

### **Minute 2-3: Apply Migration**

```powershell
# Terminal 2 - Apply database migration
cd C:\test

# Check current state
dotnet ef migrations list --project src/CleanArchitecture.Infrastructure

# Apply migration
dotnet ef database update --project src/CleanArchitecture.Infrastructure

# ✅ Watch for: "Done."
# 🔍 Tables created: regions, companies, departments, role_organization_scopes
```

**If migration fails:**
```powershell
# Check connection string in appsettings.json
# Should be: "Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;"

# Verify database is running
docker logs postgres  # Check logs
```

---

### **Minute 3-4: Start API Server**

```powershell
# Terminal 3 - Start API Server
cd C:\test
dotnet run --project src/CleanArchitecture.Api

# 🟢 Expected output:
# info: Microsoft.Hosting.Lifetime[14]
#   Now listening on: http://localhost:5000
#   Now listening on: https://localhost:5001
```

**Alternative (Visual Studio):**
- Open Visual Studio
- Right-click `CleanArchitecture.Api` project → Set as Startup Project
- Press `F5` to debug OR `Ctrl+F5` to run without debugging

---

### **Minute 4-5: Test API**

**Option A: Via Browser (Swagger UI)**
```
Navigate to: http://localhost:5000/swagger/index.html
```

**Option B: Via Postman**
```powershell
# Terminal 4 - Import Postman collection
# 1. Open Postman
# 2. Click: File → Import
# 3. Select: POSTMAN_COLLECTION.json (in your root directory)
# 4. Click: Import
# 5. Follow test sequence 1-10
```

**Option C: Via cURL**
```bash
# Test 1: Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"admin123"}'

# Response: { "accessToken": "eyJ..." }
```

---

## ✅ Verification Steps

### **Step 1: Database Created** ✅
```sql
-- Login to PostgreSQL (via pgAdmin or DBeaver)
-- Database: rbac_db

-- Run this query:
SELECT tablename FROM pg_tables 
WHERE schemaname='public' 
AND tablename IN ('regions', 'companies', 'departments', 'role_organization_scopes')
ORDER BY tablename;

-- Expected: 4 rows
-- ✅ regions
-- ✅ companies
-- ✅ departments
-- ✅ role_organization_scopes
```

### **Step 2: API Running** ✅
```bash
# Terminal: Check API response
curl http://localhost:5000/swagger/index.html
# Expected: HTTP 200 (Swagger UI loads)
```

### **Step 3: Permission Service Registered** ✅
```csharp
// In Visual Studio Terminal (or Test)
// This should not throw exception
var service = app.Services.GetRequiredService<IHierarchicalPermissionService>();
// ✅ Service resolved successfully
```

---

## 🎯 What's Running Now?

```
┌─────────────────────────────────────────────┐
│ 🟢 PostgreSQL running (port 5432)          │
│ 🟢 API Server running (port 5000)          │
│ 🟢 Swagger UI running (http://localhost...) │
│ 🟢 Database seeded (3 regions, 2 cos, 3 depts) │
│ 🟢 4 new tables created                    │
│ 🟢 Permission service registered           │
└─────────────────────────────────────────────┘
```

---

## 🧪 Quick Test Example

### **Test 1: Create a Role with Permissions**

```powershell
# First, get JWT token (or use existing)
$token = "your_jwt_token_here"

# Create role
$body = @{
    roleCode = "ChiefAccountant"
    roleName = "Kế Toán Trưởng"
    description = "Chief Accountant"
    subsystemCodes = @("Reports", "Analytics")
    permissions = @("View", "Create", "Edit")
} | ConvertTo-Json

curl -X POST http://localhost:5000/api/admin/setup-role `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d $body

# Expected: 
# {
#   "message": "Role 'Kế Toán Trưởng' setup successfully",
#   "roleId": "guid",
#   "subsystemsAssigned": 2,
#   "permissionsAssigned": ["View", "Create", "Edit"]
# }
```

### **Test 2: Check User Permissions**

```powershell
curl -X GET http://localhost:5000/api/admin/users/{userId}/effective-permissions `
  -H "Authorization: Bearer $token"

# Expected:
# {
#   "userId": "guid",
#   "userName": "John Doe",
#   "email": "john@example.com",
#   "userRoles": [
#     { "code": "ChiefAccountant", "name": "Kế Toán Trưởng" }
#   ],
#   "permissions": [
#     {
#       "subsystem": "Reports",
#       "permissions": ["View", "Create", "Edit"],
#       "roles": [...]
#     }
#   ]
# }
```

---

## 🛠️ Troubleshooting Quick Fixes

### **❌ "Connection refused" on port 5432**
```powershell
# Solution: Start Docker
docker-compose up -d
```

### **❌ "Migration not found"**
```powershell
# Solution: Migrations already created, just apply them
dotnet ef database update
```

### **❌ "Port 5000 already in use"**
```powershell
# Solution: Use different port
dotnet run --project src/CleanArchitecture.Api --urls "http://localhost:5001"
```

### **❌ "Build failed"**
```powershell
# Solution: Clean and rebuild
dotnet clean
dotnet build
```

---

## 📊 What You Can Do Now

✅ Create roles with permissions
✅ Assign roles to users
✅ Check user permissions
✅ Query roles with permissions
✅ Get effective user permissions
✅ View organizational hierarchy (regions, companies, departments)
✅ Manage role organization scopes
✅ Batch check permissions for resources

---

## 🎓 Learning Path

### **Beginner:**
1. ✅ Login (Postman: Test 1)
2. ✅ Create role (Postman: Test 2-3)
3. ✅ Assign role to user (Postman: Test 4)
4. ✅ View effective permissions (Postman: Test 6)

### **Intermediate:**
5. ✅ Understand org hierarchy (regions → companies → departments)
6. ✅ Check permission in scope (Postman: Test 8)
7. ✅ Get accessible scopes (Postman: Test 9)
8. ✅ Batch check permissions (Postman: Test 10)

### **Advanced:**
9. ✅ Implement custom permission checks in business logic
10. ✅ Add role organization scope restrictions
11. ✅ Handle hierarchical permission inheritance
12. ✅ Optimize permission queries with caching

---

## 🚀 Deploy to Production

### **Step 1: Use Production Connection String**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-postgres.company.com;Port=5432;Database=rbac_prod;User Id=prod_user;Password=****;"
  }
}
```

### **Step 2: Run Migration on Production**
```powershell
dotnet ef database update --project src/CleanArchitecture.Infrastructure --connection "Server=prod-postgres...;Database=rbac_prod;"
```

### **Step 3: Publish API**
```powershell
dotnet publish -c Release --output ./publish
```

### **Step 4: Deploy Container**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY ./publish /app
WORKDIR /app
ENTRYPOINT ["dotnet", "CleanArchitecture.Api.dll"]
```

---

## 📚 Important Files

| File | Purpose |
|------|---------|
| `IMPLEMENTATION_SUMMARY.md` | Full summary of implementation |
| `HIERARCHICAL_RBAC_IMPLEMENTATION_COMPLETE.md` | Detailed guide with examples |
| `SQL_VERIFICATION_QUERIES.sql` | Verify database setup |
| `POSTMAN_COLLECTION.json` | API test collection |
| `docker-compose.yml` | PostgreSQL + other services |

---

## ✨ You're All Set!

```
┌─ DATABASE ────────────────────┐
│ ✅ PostgreSQL running         │
│ ✅ 4 new tables created       │
│ ✅ Seeded: 3 regions, 2 cos   │
└───────────────────────────────┘
           ↓
┌─ APPLICATION ─────────────────┐
│ ✅ API server running         │
│ ✅ Swagger UI available       │
│ ✅ Auth endpoints working     │
│ ✅ Permission service active  │
└───────────────────────────────┘
           ↓
┌─ READY TO ────────────────────┐
│ ✅ Create roles               │
│ ✅ Assign users               │
│ ✅ Check permissions          │
│ ✅ Manage scopes              │
└───────────────────────────────┘
```

---

## 🎉 Congratulations!

You have a **production-ready hierarchical RBAC system** that:
- ✅ Supports 200+ reports per subsystem
- ✅ Handles multiple roles per user
- ✅ Enforces region/company/department scope restrictions
- ✅ Provides 3-tier permission checking
- ✅ Is fully tested and ready to deploy

**Start building amazing features! 🚀**

---

## 💬 Questions?

Refer to:
- **Implementation details?** → `HIERARCHICAL_RBAC_IMPLEMENTATION_COMPLETE.md`
- **Database schema?** → `SQL_VERIFICATION_QUERIES.sql`
- **API examples?** → `POSTMAN_COLLECTION.json`
- **Troubleshooting?** → Section above

**Good luck! 🍀**
