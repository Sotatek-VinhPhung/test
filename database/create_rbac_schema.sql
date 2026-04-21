-- ============================================================================
-- POSTGRESQL RBAC SCHEMA - Chạy trực tiếp trong DBeaver
-- ============================================================================
-- Drop existing tables (nếu có)
DROP TABLE IF EXISTS "RoleOrganizationScopes" CASCADE;
DROP TABLE IF EXISTS "Departments" CASCADE;
DROP TABLE IF EXISTS "Companies" CASCADE;
DROP TABLE IF EXISTS "Regions" CASCADE;
DROP TABLE IF EXISTS "UserPermissionOverrides" CASCADE;
DROP TABLE IF EXISTS "RoleSubsystemPermissions" CASCADE;
DROP TABLE IF EXISTS "UserRoles" CASCADE;
DROP TABLE IF EXISTS "Subsystems" CASCADE;
DROP TABLE IF EXISTS "Roles" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;

-- ============================================================================
-- 1. USERS TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(255) NOT NULL,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Organizational hierarchy
    "RegionId" UUID,
    "CompanyId" UUID,
    "DepartmentId" UUID
);

CREATE INDEX idx_users_email ON "Users"("Email");
CREATE INDEX idx_users_is_active ON "Users"("IsActive");

-- ============================================================================
-- 2. SUBSYSTEMS TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS "Subsystems" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_subsystems_code ON "Subsystems"("Code");
CREATE UNIQUE INDEX idx_subsystems_code_unique ON "Subsystems"("Code");

-- ============================================================================
-- 3. ROLES TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS "Roles" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_roles_code ON "Roles"("Code");
CREATE UNIQUE INDEX idx_roles_code_unique ON "Roles"("Code");

-- ============================================================================
-- 4. ROLE_SUBSYSTEM_PERMISSIONS TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS "RoleSubsystemPermissions" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "RoleId" UUID NOT NULL REFERENCES "Roles"("Id") ON DELETE CASCADE,
    "SubsystemId" UUID NOT NULL REFERENCES "Subsystems"("Id") ON DELETE CASCADE,
    "Flags" BIGINT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE("RoleId", "SubsystemId")
);

CREATE INDEX idx_role_subsystem_permissions_role_id ON "RoleSubsystemPermissions"("RoleId");
CREATE INDEX idx_role_subsystem_permissions_subsystem_id ON "RoleSubsystemPermissions"("SubsystemId");

-- ============================================================================
-- 5. USER_ROLES TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS "UserRoles" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "RoleId" UUID NOT NULL REFERENCES "Roles"("Id") ON DELETE CASCADE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE("UserId", "RoleId")
);

CREATE INDEX idx_user_roles_user_id ON "UserRoles"("UserId");
CREATE INDEX idx_user_roles_role_id ON "UserRoles"("RoleId");

-- ============================================================================
-- 6. USER_PERMISSION_OVERRIDES TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS "UserPermissionOverrides" (
    "UserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Module" VARCHAR(50) NOT NULL,
    "Flags" BIGINT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY("UserId", "Module")
);

CREATE INDEX idx_user_permission_overrides_user_id ON "UserPermissionOverrides"("UserId");

-- ============================================================================
-- 7. REGIONS TABLE (Organizational Hierarchy)
-- ============================================================================
CREATE TABLE IF NOT EXISTS "Regions" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(200) NOT NULL,
    "Country" VARCHAR(100),
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_regions_code ON "Regions"("Code");
CREATE UNIQUE INDEX idx_regions_code_unique ON "Regions"("Code");

-- Update users table to add foreign key to regions
ALTER TABLE "Users" ADD CONSTRAINT fk_users_regions 
    FOREIGN KEY ("RegionId") REFERENCES "Regions"("Id") ON DELETE SET NULL;

-- ============================================================================
-- 8. COMPANIES TABLE (Organizational Hierarchy)
-- ============================================================================
CREATE TABLE IF NOT EXISTS "Companies" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(200) NOT NULL,
    "TaxId" VARCHAR(50),
    "RegionId" UUID NOT NULL REFERENCES "Regions"("Id") ON DELETE CASCADE,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_companies_code ON "Companies"("Code");
CREATE INDEX idx_companies_region_id ON "Companies"("RegionId");
CREATE UNIQUE INDEX idx_companies_code_unique ON "Companies"("Code");

-- Update users table to add foreign key to companies
ALTER TABLE "Users" ADD CONSTRAINT fk_users_companies 
    FOREIGN KEY ("CompanyId") REFERENCES "Companies"("Id") ON DELETE SET NULL;

-- ============================================================================
-- 9. DEPARTMENTS TABLE (Organizational Hierarchy)
-- ============================================================================
CREATE TABLE IF NOT EXISTS "Departments" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "CompanyId" UUID NOT NULL REFERENCES "Companies"("Id") ON DELETE CASCADE,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE("Code", "CompanyId")
);

CREATE INDEX idx_departments_code ON "Departments"("Code");
CREATE INDEX idx_departments_company_id ON "Departments"("CompanyId");

-- Update users table to add foreign key to departments
ALTER TABLE "Users" ADD CONSTRAINT fk_users_departments 
    FOREIGN KEY ("DepartmentId") REFERENCES "Departments"("Id") ON DELETE SET NULL;

-- ============================================================================
-- 10. ROLE_ORGANIZATION_SCOPES TABLE (ABAC - Attribute-Based Access Control)
-- ============================================================================
CREATE TABLE IF NOT EXISTS "RoleOrganizationScopes" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "RoleId" UUID NOT NULL REFERENCES "Roles"("Id") ON DELETE CASCADE,
    "RegionId" UUID REFERENCES "Regions"("Id") ON DELETE CASCADE,
    "CompanyId" UUID REFERENCES "Companies"("Id") ON DELETE CASCADE,
    "DepartmentId" UUID REFERENCES "Departments"("Id") ON DELETE CASCADE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE("RoleId", "RegionId", "CompanyId", "DepartmentId")
);

CREATE INDEX idx_role_org_scopes_role_id ON "RoleOrganizationScopes"("RoleId");
CREATE INDEX idx_role_org_scopes_region_id ON "RoleOrganizationScopes"("RegionId");
CREATE INDEX idx_role_org_scopes_company_id ON "RoleOrganizationScopes"("CompanyId");
CREATE INDEX idx_role_org_scopes_department_id ON "RoleOrganizationScopes"("DepartmentId");

-- ============================================================================
-- SEED DATA
-- ============================================================================

-- Insert Subsystems (Reports, Users Management, Settings)
INSERT INTO "Subsystems" ("Id", "Code", "Name", "Description", "IsActive") VALUES
    (gen_random_uuid(), 'REPORT_SUBSYSTEM', 'Reports Subsystem', '200+ Reports Management', true),
    (gen_random_uuid(), 'USER_SUBSYSTEM', 'User Management', 'User and Role Management', true),
    (gen_random_uuid(), 'SETTINGS_SUBSYSTEM', 'Settings', 'System Settings', true);

-- Insert Regions
INSERT INTO "Regions" ("Id", "Code", "Name", "Country", "IsActive") VALUES
    ('10000000-0000-0000-0000-000000000001', 'VN-N', 'Vietnam - North', 'Vietnam', true),
    ('10000000-0000-0000-0000-000000000002', 'VN-S', 'Vietnam - South', 'Vietnam', true),
    ('10000000-0000-0000-0000-000000000003', 'SG', 'Singapore', 'Singapore', true);

-- Insert Companies
INSERT INTO "Companies" ("Id", "Code", "Name", "TaxId", "RegionId", "IsActive") VALUES
    ('20000000-0000-0000-0000-000000000001', 'ABC-CORP', 'ABC Corporation', 'ABC123456', '10000000-0000-0000-0000-000000000001', true),
    ('20000000-0000-0000-0000-000000000002', 'XYZ-TECH', 'XYZ Technology', 'XYZ789012', '10000000-0000-0000-0000-000000000002', true);

-- Insert Departments
INSERT INTO "Departments" ("Id", "Code", "Name", "CompanyId", "IsActive") VALUES
    ('30000000-0000-0000-0000-000000000001', 'ACC', 'Accounting', '20000000-0000-0000-0000-000000000001', true),
    ('30000000-0000-0000-0000-000000000002', 'HR', 'Human Resources', '20000000-0000-0000-0000-000000000001', true),
    ('30000000-0000-0000-0000-000000000003', 'IT', 'IT Support', '20000000-0000-0000-0000-000000000002', true);

-- Insert Roles (Admin, Manager, Editor, Viewer)
INSERT INTO "Roles" ("Id", "Code", "Name", "Description", "IsActive") VALUES
    ('40000000-0000-0000-0000-000000000001', 'ADMIN', 'Administrator', 'Full system access', true),
    ('40000000-0000-0000-0000-000000000002', 'MANAGER', 'Manager', 'Department/Company management', true),
    ('40000000-0000-0000-0000-000000000003', 'EDITOR', 'Editor', 'Can edit reports and data', true),
    ('40000000-0000-0000-0000-000000000004', 'VIEWER', 'Viewer', 'Read-only access', true);

-- Insert Role-Subsystem Permissions
-- Admin: All permissions on all subsystems (flags = 9223372036854775807 = max int64)
INSERT INTO "RoleSubsystemPermissions" ("Id", "RoleId", "SubsystemId", "Flags") VALUES
    (gen_random_uuid(), '40000000-0000-0000-0000-000000000001', (SELECT "Id" FROM "Subsystems" WHERE "Code" = 'REPORT_SUBSYSTEM'), 9223372036854775807),
    (gen_random_uuid(), '40000000-0000-0000-0000-000000000001', (SELECT "Id" FROM "Subsystems" WHERE "Code" = 'USER_SUBSYSTEM'), 9223372036854775807),
    (gen_random_uuid(), '40000000-0000-0000-0000-000000000001', (SELECT "Id" FROM "Subsystems" WHERE "Code" = 'SETTINGS_SUBSYSTEM'), 9223372036854775807);

-- Manager: Read + Create + Update (flags = 28 = 0x1C)
INSERT INTO "RoleSubsystemPermissions" ("Id", "RoleId", "SubsystemId", "Flags") VALUES
    (gen_random_uuid(), '40000000-0000-0000-0000-000000000002', (SELECT "Id" FROM "Subsystems" WHERE "Code" = 'REPORT_SUBSYSTEM'), 28),
    (gen_random_uuid(), '40000000-0000-0000-0000-000000000002', (SELECT "Id" FROM "Subsystems" WHERE "Code" = 'USER_SUBSYSTEM'), 28);

-- Editor: Read + Create + Update (flags = 28 = 0x1C)
INSERT INTO "RoleSubsystemPermissions" ("Id", "RoleId", "SubsystemId", "Flags") VALUES
    (gen_random_uuid(), '40000000-0000-0000-0000-000000000003', (SELECT "Id" FROM "Subsystems" WHERE "Code" = 'REPORT_SUBSYSTEM'), 28);

-- Viewer: Read only (flags = 1 = 0x01)
INSERT INTO "RoleSubsystemPermissions" ("Id", "RoleId", "SubsystemId", "Flags") VALUES
    (gen_random_uuid(), '40000000-0000-0000-0000-000000000004', (SELECT "Id" FROM "Subsystems" WHERE "Code" = 'REPORT_SUBSYSTEM'), 1);

-- Insert Role Organization Scopes (ABAC)
-- Admin: All regions
INSERT INTO "RoleOrganizationScopes" ("Id", "RoleId", "RegionId") VALUES
    (gen_random_uuid(), '40000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001'),
    (gen_random_uuid(), '40000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002'),
    (gen_random_uuid(), '40000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000003');

-- Manager: Vietnam - North only
INSERT INTO "RoleOrganizationScopes" ("Id", "RoleId", "RegionId") VALUES
    (gen_random_uuid(), '40000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001');

-- Create test user (Admin)
INSERT INTO "Users" ("Id", "FirstName", "LastName", "Email", "PasswordHash", "IsActive", "RegionId", "CompanyId", "DepartmentId") VALUES
    ('50000000-0000-0000-0000-000000000001', 'Admin', 'User', 'admin@rbac.com', '$2a$11$9O9HNiFhT8C7w5FgRk7gROzs5OPIz.FI8M2vjL5DzXBF1g68YqCYm', true, '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001');

-- Assign Admin role to test user
INSERT INTO "UserRoles" ("Id", "UserId", "RoleId") VALUES
    (gen_random_uuid(), '50000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001');

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================
-- Verify table creation
-- SELECT tablename FROM pg_tables WHERE schemaname = 'public';

-- Verify data
-- SELECT COUNT(*) as users_count FROM "Users";
-- SELECT COUNT(*) as roles_count FROM "Roles";
-- SELECT COUNT(*) as subsystems_count FROM "Subsystems";
-- SELECT COUNT(*) as regions_count FROM "Regions";
-- SELECT COUNT(*) as companies_count FROM "Companies";
-- SELECT COUNT(*) as departments_count FROM "Departments";
