# 🚀 HƯỚNG DẪN SETUP DATABASE BẰNG SQL - DBEAVER

## 📋 Các Bước Thực Hiện

### **Bước 1: Tắt API Server**
- Đừng chạy `dotnet run --project src/CleanArchitecture.Api` 
- Nếu đang chạy thì tắt nó đi (Ctrl+C)

### **Bước 2: Mở DBeaver**
1. Kết nối đến PostgreSQL server:
   - **Server**: localhost (hoặc IP)
   - **Port**: 5432
   - **Username**: postgres
   - **Password**: 123456

2. Nếu database `rbac_db` tồn tại, hãy **xóa nó**:
   ```sql
   DROP DATABASE IF EXISTS rbac_db;
   ```

3. Tạo database mới:
   ```sql
   CREATE DATABASE rbac_db WITH OWNER postgres;
   ```

### **Bước 3: Chạy SQL Script**
1. Mở file: `database/create_rbac_schema.sql`
2. **Select All** (Ctrl+A)
3. Nhấn **Execute** (hoặc Ctrl+Enter)
4. Chờ cho đến khi hoàn thành ✅

### **Bước 4: Xác Minh**
Chạy các query này để kiểm tra:

```sql
-- 1. Liệt kê tất cả tables
SELECT tablename FROM pg_tables WHERE schemaname = 'public';

-- 2. Đếm records
SELECT 'users' as table_name, COUNT(*) as count FROM "users"
UNION ALL SELECT 'roles', COUNT(*) FROM "roles"
UNION ALL SELECT 'subsystems', COUNT(*) FROM "subsystems"
UNION ALL SELECT 'regions', COUNT(*) FROM "regions"
UNION ALL SELECT 'companies', COUNT(*) FROM "companies"
UNION ALL SELECT 'departments', COUNT(*) FROM "departments"
UNION ALL SELECT 'user_roles', COUNT(*) FROM "user_roles"
UNION ALL SELECT 'role_subsystem_permissions', COUNT(*) FROM "role_subsystem_permissions";

-- 3. Kiểm tra test user
SELECT "id", "email", "first_name", "last_name" FROM "users";

-- 4. Kiểm tra roles
SELECT "code", "name" FROM "roles";
```

### **Bước 5: Chạy API**
```powershell
dotnet run --project src/CleanArchitecture.Api
```

Bạn sẽ thấy log:
```
✅ Database connection successful
📝 NOTE: Run database/create_rbac_schema.sql in DBeaver to create schema
```

## 🔑 Test User (đã được tạo)
- **Email**: admin@rbac.com
- **Password**: Admin@123 (bcrypt hash)
- **Role**: Admin
- **Quyền**: Full access

## 📊 Database Schema

### Tables:
1. **users** - 1 user (admin)
2. **roles** - 4 roles (Admin, Manager, Editor, Viewer)
3. **subsystems** - 3 subsystems (Reports, Users, Settings)
4. **regions** - 3 regions (VN-N, VN-S, SG)
5. **companies** - 2 companies (ABC-Corp, XYZ-Tech)
6. **departments** - 3 departments (Accounting, HR, IT)
7. **user_roles** - 1 assignment
8. **role_subsystem_permissions** - 8 assignments
9. **role_organization_scopes** - 3 scopes
10. **user_permission_overrides** - (empty, ready for use)

## ✅ Kiểm Tra Lỗi

Nếu gặp lỗi trong DBeaver:
1. **"relation already exists"** → Xóa database, tạo lại
2. **"permission denied"** → Kiểm tra username/password
3. **"could not connect"** → Kiểm tra PostgreSQL đang chạy (`docker-compose up -d`)

## 🔧 Rebuild/Clean
Nếu vẫn gặp lỗi migration:
```powershell
# Clean build
dotnet clean src/CleanArchitecture.Infrastructure
dotnet build src/CleanArchitecture.Infrastructure

# Then run API
dotnet run --project src/CleanArchitecture.Api
```

---

**Lưu ý**: Migration files đã bị xóa, sử dụng SQL script thay thế!
