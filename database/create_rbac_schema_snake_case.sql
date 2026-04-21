-- PostgreSQL RBAC Schema with snake_case naming convention
-- All table and column names follow: lowercase_with_underscores

-- Drop tables in reverse order of dependencies
DROP TABLE IF EXISTS "exported_files" CASCADE;
DROP TABLE IF EXISTS "role_organization_scopes" CASCADE;
DROP TABLE IF EXISTS "user_permission_overrides" CASCADE;
DROP TABLE IF EXISTS "role_subsystem_permissions" CASCADE;
DROP TABLE IF EXISTS "user_roles" CASCADE;
DROP TABLE IF EXISTS "departments" CASCADE;
DROP TABLE IF EXISTS "companies" CASCADE;
DROP TABLE IF EXISTS "regions" CASCADE;
DROP TABLE IF EXISTS "subsystems" CASCADE;
DROP TABLE IF EXISTS "roles" CASCADE;
DROP TABLE IF EXISTS "users" CASCADE;

-- ============================================================
-- Users Table (required - base entity)
-- ============================================================
CREATE TABLE IF NOT EXISTS "users" (
    "Id" UUID PRIMARY KEY,
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(256) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(500) NOT NULL,
    "Role" VARCHAR(20) DEFAULT 'User',
    "RefreshToken" VARCHAR(256),
    "RegionId" UUID,
    "CompanyId" UUID,
    "DepartmentId" UUID,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "idx_users_email" ON "users"("Email");
CREATE INDEX "idx_users_is_active" ON "users"("IsActive");
CREATE INDEX "idx_users_region_id" ON "users"("RegionId");
CREATE INDEX "idx_users_company_id" ON "users"("CompanyId");
CREATE INDEX "idx_users_department_id" ON "users"("DepartmentId");

-- ============================================================
-- Roles Table (RBAC - roles that users can have)
-- ============================================================
CREATE TABLE IF NOT EXISTS "roles" (
    "Id" UUID PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(100) NOT NULL,
    "Description" TEXT,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "idx_roles_code" ON "roles"("Code");
CREATE INDEX "idx_roles_is_active" ON "roles"("IsActive");

-- ============================================================
-- Subsystems Table (system modules/features)
-- ============================================================
CREATE TABLE IF NOT EXISTS "subsystems" (
    "Id" UUID PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(100) NOT NULL,
    "Description" TEXT,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "idx_subsystems_code" ON "subsystems"("Code");
CREATE INDEX "idx_subsystems_is_active" ON "subsystems"("IsActive");

-- ============================================================
-- UserRoles Table (many-to-many: Users ← → Roles)
-- ============================================================
CREATE TABLE IF NOT EXISTS "user_roles" (
    "UserId" UUID NOT NULL,
    "RoleId" UUID NOT NULL,
    "AssignedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY ("UserId", "RoleId"),
    FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("RoleId") REFERENCES "roles"("Id") ON DELETE CASCADE
);

CREATE INDEX "idx_user_roles_user_id" ON "user_roles"("UserId");
CREATE INDEX "idx_user_roles_role_id" ON "user_roles"("RoleId");

-- ============================================================
-- RoleSubsystemPermissions Table 
-- (defines what permissions a role has on each subsystem)
-- ============================================================
CREATE TABLE IF NOT EXISTS "role_subsystem_permissions" (
    "RoleId" UUID NOT NULL,
    "SubsystemId" UUID NOT NULL,
    "Flags" BIGINT DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY ("RoleId", "SubsystemId"),
    FOREIGN KEY ("RoleId") REFERENCES "roles"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("SubsystemId") REFERENCES "subsystems"("Id") ON DELETE CASCADE
);

CREATE INDEX "idx_role_subsystem_permissions_role_id" ON "role_subsystem_permissions"("RoleId");
CREATE INDEX "idx_role_subsystem_permissions_subsystem_id" ON "role_subsystem_permissions"("SubsystemId");

-- ============================================================
-- UserPermissionOverrides Table
-- (allows per-user permission override/grant outside of roles)
-- ============================================================
CREATE TABLE IF NOT EXISTS "user_permission_overrides" (
    "UserId" UUID NOT NULL,
    "Module" VARCHAR(50) NOT NULL,
    "Flags" BIGINT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY ("UserId", "Module"),
    FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE
);

CREATE INDEX "idx_user_permission_overrides_user_id" ON "user_permission_overrides"("UserId");

-- ============================================================
-- Regions Table (hierarchical organizational structure level 1)
-- ============================================================
CREATE TABLE IF NOT EXISTS "regions" (
    "Id" UUID PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(200) NOT NULL,
    "Country" VARCHAR(100) NOT NULL,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX "idx_regions_code" ON "regions"("Code");
CREATE INDEX "idx_regions_is_active" ON "regions"("IsActive");

-- ============================================================
-- Companies Table (hierarchical organizational structure level 2)
-- ============================================================
CREATE TABLE IF NOT EXISTS "companies" (
    "Id" UUID PRIMARY KEY,
    "RegionId" UUID,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(200) NOT NULL,
    "TaxId" VARCHAR(50) NOT NULL UNIQUE,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("RegionId") REFERENCES "regions"("Id") ON DELETE SET NULL
);

CREATE INDEX "idx_companies_code" ON "companies"("Code");
CREATE INDEX "idx_companies_tax_id" ON "companies"("TaxId");
CREATE INDEX "idx_companies_region_id" ON "companies"("RegionId");
CREATE INDEX "idx_companies_is_active" ON "companies"("IsActive");

-- ============================================================
-- Departments Table (hierarchical organizational structure level 3)
-- ============================================================
CREATE TABLE IF NOT EXISTS "departments" (
    "Id" UUID PRIMARY KEY,
    "CompanyId" UUID NOT NULL,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(200) NOT NULL,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CompanyId") REFERENCES "companies"("Id") ON DELETE CASCADE
);

CREATE INDEX "idx_departments_code" ON "departments"("Code");
CREATE INDEX "idx_departments_company_id" ON "departments"("CompanyId");
CREATE INDEX "idx_departments_is_active" ON "departments"("IsActive");

-- ============================================================
-- RoleOrganizationScopes Table
-- (defines which organization levels a role is scoped to: region, company, department)
-- ============================================================
CREATE TABLE IF NOT EXISTS "role_organization_scopes" (
    "Id" UUID PRIMARY KEY,
    "RoleId" UUID NOT NULL,
    "RegionId" UUID,
    "CompanyId" UUID,
    "DepartmentId" UUID,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("RoleId") REFERENCES "roles"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("RegionId") REFERENCES "regions"("Id") ON DELETE SET NULL,
    FOREIGN KEY ("CompanyId") REFERENCES "companies"("Id") ON DELETE SET NULL,
    FOREIGN KEY ("DepartmentId") REFERENCES "departments"("Id") ON DELETE SET NULL
);

CREATE INDEX "idx_role_organization_scopes_role_id" ON "role_organization_scopes"("RoleId");
CREATE INDEX "idx_role_organization_scopes_region_id" ON "role_organization_scopes"("RegionId");
CREATE INDEX "idx_role_organization_scopes_company_id" ON "role_organization_scopes"("CompanyId");
CREATE INDEX "idx_role_organization_scopes_department_id" ON "role_organization_scopes"("DepartmentId");

-- ============================================================
-- SEED DATA
-- ============================================================

-- Insert Regions
INSERT INTO "regions" ("Id", "Code", "Name", "Country", "IsActive")
VALUES 
    ('550e8400-e29b-41d4-a716-446655440001'::uuid, 'REGION_NORTH', 'North Vietnam', 'Vietnam', true),
    ('550e8400-e29b-41d4-a716-446655440002'::uuid, 'REGION_SOUTH', 'South Vietnam', 'Vietnam', true),
    ('550e8400-e29b-41d4-a716-446655440003'::uuid, 'REGION_CENTRAL', 'Central Vietnam', 'Vietnam', true);

-- Insert Companies
INSERT INTO "companies" ("Id", "RegionId", "Code", "Name", "TaxId", "IsActive")
VALUES 
    ('550e8400-e29b-41d4-a716-446655440011'::uuid, '550e8400-e29b-41d4-a716-446655440001'::uuid, 'COMPANY_A', 'Company A', 'TAX001', true),
    ('550e8400-e29b-41d4-a716-446655440012'::uuid, '550e8400-e29b-41d4-a716-446655440002'::uuid, 'COMPANY_B', 'Company B', 'TAX002', true);

-- Insert Departments
INSERT INTO "departments" ("Id", "CompanyId", "Code", "Name", "IsActive")
VALUES 
    ('550e8400-e29b-41d4-a716-446655440021'::uuid, '550e8400-e29b-41d4-a716-446655440011'::uuid, 'DEPT_IT', 'IT Department', true),
    ('550e8400-e29b-41d4-a716-446655440022'::uuid, '550e8400-e29b-41d4-a716-446655440011'::uuid, 'DEPT_HR', 'HR Department', true),
    ('550e8400-e29b-41d4-a716-446655440023'::uuid, '550e8400-e29b-41d4-a716-446655440012'::uuid, 'DEPT_FINANCE', 'Finance Department', true);

-- Insert Roles
INSERT INTO "roles" ("Id", "Code", "Name", "Description", "IsActive")
VALUES 
    ('550e8400-e29b-41d4-a716-446655440031'::uuid, 'ADMIN', 'Administrator', 'Full system access', true),
    ('550e8400-e29b-41d4-a716-446655440032'::uuid, 'MANAGER', 'Manager', 'Department/Company manager', true),
    ('550e8400-e29b-41d4-a716-446655440033'::uuid, 'EDITOR', 'Editor', 'Can edit content', true),
    ('550e8400-e29b-41d4-a716-446655440034'::uuid, 'VIEWER', 'Viewer', 'Read-only access', true);

-- Insert Subsystems
INSERT INTO "subsystems" ("Id", "Code", "Name", "Description", "IsActive")
VALUES 
    ('550e8400-e29b-41d4-a716-446655440041'::uuid, 'USER_MANAGEMENT', 'User Management', 'User CRUD and role assignment', true),
    ('550e8400-e29b-41d4-a716-446655440042'::uuid, 'PERMISSION_MANAGEMENT', 'Permission Management', 'Permission configuration', true),
    ('550e8400-e29b-41d4-a716-446655440043'::uuid, 'REPORTING', 'Reporting', 'Report generation and viewing', true),
    ('550e8400-e29b-41d4-a716-446655440044'::uuid, 'ANALYTICS', 'Analytics', 'Data analytics and dashboards', true);

-- Insert Users
INSERT INTO "users" ("Id", "FirstName", "LastName", "Email", "PasswordHash", "Role", "RegionId", "CompanyId", "DepartmentId", "IsActive")
VALUES 
    ('550e8400-e29b-41d4-a716-446655440051'::uuid, 'Admin', 'User', 'admin@test.com', '$2a$11$hash', 'Admin', '550e8400-e29b-41d4-a716-446655440001'::uuid, '550e8400-e29b-41d4-a716-446655440011'::uuid, '550e8400-e29b-41d4-a716-446655440021'::uuid, true);

-- Insert UserRoles
INSERT INTO "user_roles" ("UserId", "RoleId")
VALUES 
    ('550e8400-e29b-41d4-a716-446655440051'::uuid, '550e8400-e29b-41d4-a716-446655440031'::uuid);

-- Insert RoleSubsystemPermissions (permission flags)
-- Flags: 1=Create, 2=Read, 4=Update, 8=Delete, 16=Approve, 32=Admin
INSERT INTO "role_subsystem_permissions" ("RoleId", "SubsystemId", "Flags")
VALUES 
    -- Admin: All permissions on all subsystems (63 = all flags)
    ('550e8400-e29b-41d4-a716-446655440031'::uuid, '550e8400-e29b-41d4-a716-446655440041'::uuid, 63),
    ('550e8400-e29b-41d4-a716-446655440031'::uuid, '550e8400-e29b-41d4-a716-446655440042'::uuid, 63),
    ('550e8400-e29b-41d4-a716-446655440031'::uuid, '550e8400-e29b-41d4-a716-446655440043'::uuid, 63),
    ('550e8400-e29b-41d4-a716-446655440031'::uuid, '550e8400-e29b-41d4-a716-446655440044'::uuid, 63),
    -- Manager: Create, Read, Update (7 = 1+2+4)
    ('550e8400-e29b-41d4-a716-446655440032'::uuid, '550e8400-e29b-41d4-a716-446655440041'::uuid, 7),
    ('550e8400-e29b-41d4-a716-446655440032'::uuid, '550e8400-e29b-41d4-a716-446655440043'::uuid, 7),
    -- Editor: Create, Read, Update (7)
    ('550e8400-e29b-41d4-a716-446655440033'::uuid, '550e8400-e29b-41d4-a716-446655440043'::uuid, 7),
    ('550e8400-e29b-41d4-a716-446655440033'::uuid, '550e8400-e29b-41d4-a716-446655440044'::uuid, 7),
    -- Viewer: Read only (2)
    ('550e8400-e29b-41d4-a716-446655440034'::uuid, '550e8400-e29b-41d4-a716-446655440043'::uuid, 2),
    ('550e8400-e29b-41d4-a716-446655440034'::uuid, '550e8400-e29b-41d4-a716-446655440044'::uuid, 2);

-- Insert RoleOrganizationScopes
INSERT INTO "role_organization_scopes" ("Id", "RoleId", "RegionId", "CompanyId", "DepartmentId")
VALUES 
    ('550e8400-e29b-41d4-a716-446655440061'::uuid, '550e8400-e29b-41d4-a716-446655440031'::uuid, NULL, NULL, NULL), -- Admin: no scope restrictions
    ('550e8400-e29b-41d4-a716-446655440062'::uuid, '550e8400-e29b-41d4-a716-446655440032'::uuid, '550e8400-e29b-41d4-a716-446655440001'::uuid, '550e8400-e29b-41d4-a716-446655440011'::uuid, NULL), -- Manager: scoped to region+company
    ('550e8400-e29b-41d4-a716-446655440063'::uuid, '550e8400-e29b-41d4-a716-446655440033'::uuid, NULL, '550e8400-e29b-41d4-a716-446655440011'::uuid, NULL), -- Editor: scoped to company
    ('550e8400-e29b-41d4-a716-446655440064'::uuid, '550e8400-e29b-41d4-a716-446655440034'::uuid, NULL, NULL, '550e8400-e29b-41d4-a716-446655440021'::uuid); -- Viewer: scoped to department

-- Add foreign keys to users table for organizational hierarchy
ALTER TABLE "users" ADD CONSTRAINT "fk_users_region_id" 
    FOREIGN KEY ("RegionId") REFERENCES "regions"("Id") ON DELETE SET NULL;

ALTER TABLE "users" ADD CONSTRAINT "fk_users_company_id"
    FOREIGN KEY ("CompanyId") REFERENCES "companies"("Id") ON DELETE SET NULL;

ALTER TABLE "users" ADD CONSTRAINT "fk_users_department_id"
    FOREIGN KEY ("DepartmentId") REFERENCES "departments"("Id") ON DELETE SET NULL;

-- ============================================================
-- ExportedFiles Table (export feature - Excel, Word, PDF to MinIO)
-- ============================================================
CREATE TABLE IF NOT EXISTS "exported_files" (
    "Id" UUID PRIMARY KEY,
    "FileName" VARCHAR(255) NOT NULL,
    "Url" VARCHAR(2048) NOT NULL,
    "Bucket" VARCHAR(100) NOT NULL,
    "Size" BIGINT NOT NULL,
    "FileType" VARCHAR(50) NOT NULL,
    "ObjectName" VARCHAR(1024) NOT NULL,
    "UserId" UUID NOT NULL,
    "Note" VARCHAR(1000),
    "ExpiresAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN DEFAULT true,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE
);

CREATE INDEX "idx_exported_files_user_id" ON "exported_files"("UserId");
CREATE INDEX "idx_exported_files_created_at" ON "exported_files"("CreatedAt");
CREATE INDEX "idx_exported_files_expires_at" ON "exported_files"("ExpiresAt");
CREATE INDEX "idx_exported_files_bucket" ON "exported_files"("Bucket");
CREATE INDEX "idx_exported_files_is_active" ON "exported_files"("IsActive");
