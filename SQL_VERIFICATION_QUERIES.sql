-- SQL Verification Queries for Hierarchical RBAC Implementation
-- Run these after: dotnet ef database update

-- ✅ CHECK 1: Tables Created
SELECT tablename 
FROM pg_tables 
WHERE schemaname = 'public' 
  AND tablename IN ('regions', 'companies', 'departments', 'role_organization_scopes', 'users')
ORDER BY tablename;

-- Expected output: 5 rows (regions, companies, departments, role_organization_scopes, users)

---

-- ✅ CHECK 2: User Table - New Columns
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'users'
  AND column_name IN ('region_id', 'company_id', 'department_id')
ORDER BY column_name;

-- Expected output: 3 rows with uuid, yes for is_nullable

---

-- ✅ CHECK 3: Regions - Sample Data (After Seeding)
SELECT id, code, name, country, is_active
FROM regions
ORDER BY code;

-- Expected output:
-- 20000000-0000-0000-0000-000000000001 | VN-HN | Hanoi | Vietnam | true
-- 20000000-0000-0000-0000-000000000002 | VN-HCM | Ho Chi Minh City | Vietnam | true
-- 20000000-0000-0000-0000-000000000003 | SG | Singapore | Singapore | true

---

-- ✅ CHECK 4: Companies - Sample Data
SELECT id, code, name, tax_id, region_id, is_active
FROM companies
ORDER BY code;

-- Expected output:
-- 30000000-0000-0000-0000-000000000001 | ABC-CORP | ABC Corporation | 0123456789 | 20000000-0000-0000-0000-000000000001 | true
-- 30000000-0000-0000-0000-000000000002 | XYZ-TECH | XYZ Technology | 9876543210 | 20000000-0000-0000-0000-000000000002 | true

---

-- ✅ CHECK 5: Departments - Sample Data
SELECT id, code, name, company_id, is_active
FROM departments
ORDER BY code;

-- Expected output: 3 departments (Accounting, HR, IT Support)

---

-- ✅ CHECK 6: Role Organization Scopes
SELECT id, role_id, region_id, company_id, department_id, is_active
FROM role_organization_scopes
ORDER BY created_at;

-- Expected output: 2 scopes (Manager, Editor with scope restrictions)

---

-- ✅ CHECK 7: Foreign Keys - User to Organizations
SELECT constraint_name, table_name, column_name, referenced_table_name, referenced_column_name
FROM information_schema.key_column_usage
WHERE table_name = 'users' 
  AND column_name IN ('region_id', 'company_id', 'department_id')
ORDER BY column_name;

-- Expected output: 3 foreign key constraints

---

-- ✅ CHECK 8: Indexes Created
SELECT indexname, tablename
FROM pg_indexes
WHERE tablename IN ('regions', 'companies', 'departments', 'role_organization_scopes', 'users')
  AND indexname LIKE '%_Key%' OR indexname LIKE '%_Idx%'
ORDER BY tablename, indexname;

-- Expected output: All defined indexes created

---

-- ✅ CHECK 9: Verify Role Has Organization Scopes
SELECT r.code, r.name, 
       ros.region_id, ros.company_id, ros.department_id,
       reg.code as region_code, 
       co.code as company_code, 
       d.code as department_code
FROM roles r
LEFT JOIN role_organization_scopes ros ON r.id = ros.role_id AND ros.is_active = true
LEFT JOIN regions reg ON ros.region_id = reg.id
LEFT JOIN companies co ON ros.company_id = co.id
LEFT JOIN departments d ON ros.department_id = d.id
WHERE r.code IN ('Manager', 'Editor')
ORDER BY r.code, ros.created_at;

-- Expected output: Manager role restricted to Hanoi|ABC-Corp, Editor restricted to Accounting department

---

-- ✅ CHECK 10: Full Hierarchical View
SELECT 
    u.email as user_email,
    u.first_name,
    u.last_name,
    reg.code as user_region,
    co.code as user_company,
    d.code as user_department,
    r.code as role_code,
    r.name as role_name,
    ros.region_id is not null as has_scope_restriction
FROM users u
LEFT JOIN regions reg ON u.region_id = reg.id
LEFT JOIN companies co ON u.company_id = co.id
LEFT JOIN departments d ON u.department_id = d.id
LEFT JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = true
LEFT JOIN roles r ON ur.role_id = r.id
WHERE u.id IS NOT NULL
ORDER BY u.email, r.code;

-- Expected output: User organization context + their roles

---

-- ✅ PERFORMANCE: Explain Query for Permission Check
EXPLAIN ANALYZE
SELECT r.code, r.name, rsp.flags
FROM users u
JOIN user_roles ur ON u.id = ur.user_id AND ur.is_active = true
JOIN roles r ON ur.role_id = r.id
JOIN role_subsystem_permissions rsp ON r.id = rsp.role_id
WHERE u.id = '50000000-0000-0000-0000-000000000001'::uuid
  AND rsp.subsystem_id = '00000000-0000-0000-0000-000000000001'::uuid;

-- Expected: Fast execution with index usage

---

-- ✅ DATA INTEGRITY: Check for Orphaned Records
SELECT 'Users with invalid region' as check_name, COUNT(*) as count
FROM users WHERE region_id IS NOT NULL AND region_id NOT IN (SELECT id FROM regions)
UNION ALL
SELECT 'Users with invalid company', COUNT(*)
FROM users WHERE company_id IS NOT NULL AND company_id NOT IN (SELECT id FROM companies)
UNION ALL
SELECT 'Users with invalid department', COUNT(*)
FROM users WHERE department_id IS NOT NULL AND department_id NOT IN (SELECT id FROM departments)
UNION ALL
SELECT 'Role scopes with invalid role', COUNT(*)
FROM role_organization_scopes WHERE role_id NOT IN (SELECT id FROM roles)
UNION ALL
SELECT 'Role scopes with invalid region', COUNT(*)
FROM role_organization_scopes WHERE region_id IS NOT NULL AND region_id NOT IN (SELECT id FROM regions)
UNION ALL
SELECT 'Role scopes with invalid company', COUNT(*)
FROM role_organization_scopes WHERE company_id IS NOT NULL AND company_id NOT IN (SELECT id FROM companies)
UNION ALL
SELECT 'Role scopes with invalid department', COUNT(*)
FROM role_organization_scopes WHERE department_id IS NOT NULL AND department_id NOT IN (SELECT id FROM departments);

-- Expected: All counts = 0 (no orphaned records)

---

-- ✅ MIGRATION VERIFICATION
SELECT version, installed_on
FROM __efmigrationshistory
ORDER BY version DESC
LIMIT 5;

-- Expected: 20260415013644_AddOrganizationalHierarchy in the list

---

-- 📊 SUMMARY: Quick Stats
SELECT 
    (SELECT COUNT(*) FROM regions) as total_regions,
    (SELECT COUNT(*) FROM companies) as total_companies,
    (SELECT COUNT(*) FROM departments) as total_departments,
    (SELECT COUNT(*) FROM role_organization_scopes) as role_scopes,
    (SELECT COUNT(*) FROM users) as total_users,
    (SELECT COUNT(*) FROM roles) as total_roles;

-- Expected: Regions=3, Companies=2, Departments=3, RoleScopes=2, Users=?, Roles=4
