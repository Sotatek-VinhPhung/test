-- RBAC System Setup Script (PostgreSQL)
-- This script creates the new subsystem-based RBAC tables and seeds sample data
-- Version 2: Simplified with proper statement separators

-- =====================================================
-- 1. CREATE SUBSYSTEMS TABLE
-- =====================================================

CREATE TABLE IF NOT EXISTS subsystems (
    "Id" UUID PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_Subsystems_Code ON subsystems("Code");

-- =====================================================
-- 2. CREATE ROLES TABLE
-- =====================================================

CREATE TABLE IF NOT EXISTS roles (
    "Id" UUID PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_Roles_Code ON roles("Code");

-- =====================================================
-- 3. CREATE USER_ROLES JUNCTION TABLE
-- =====================================================

CREATE TABLE IF NOT EXISTS user_roles (
    "UserId" UUID NOT NULL,
    "RoleId" UUID NOT NULL,
    "AssignedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NULL,
    PRIMARY KEY ("UserId", "RoleId"),
    FOREIGN KEY ("RoleId") REFERENCES roles("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_UserRole_UserId ON user_roles("UserId");
CREATE INDEX IF NOT EXISTS IX_UserRole_RoleId ON user_roles("RoleId");

-- Add FK constraint to Users table separately (to handle case when Users doesn't exist yet)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Users' OR table_name = 'AspNetUsers') THEN
        ALTER TABLE user_roles ADD CONSTRAINT fk_user_roles_user_id 
            FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
    END IF;
EXCEPTION WHEN OTHERS THEN
    NULL; -- Ignore if constraint already exists
END
$$;

-- =====================================================
-- 4. CREATE ROLE_SUBSYSTEM_PERMISSIONS TABLE
-- =====================================================

CREATE TABLE IF NOT EXISTS role_subsystem_permissions (
    "RoleId" UUID NOT NULL,
    "SubsystemId" UUID NOT NULL,
    "Flags" BIGINT NOT NULL DEFAULT 0,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY ("RoleId", "SubsystemId"),
    FOREIGN KEY ("RoleId") REFERENCES roles("Id") ON DELETE CASCADE,
    FOREIGN KEY ("SubsystemId") REFERENCES subsystems("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_RoleSubsystemPermission_SubsystemId ON role_subsystem_permissions("SubsystemId");
CREATE INDEX IF NOT EXISTS IX_RoleSubsystemPermission_RoleId ON role_subsystem_permissions("RoleId");

-- =====================================================
-- 5. SEED SUBSYSTEMS
-- =====================================================

INSERT INTO subsystems ("Id", "Code", "Name", "Description", "IsActive", "CreatedAt")
VALUES
    ('00000000-0000-0000-0000-000000000001'::UUID, 'Reports', 'Reports Module', 'Access to reports and dashboards', true, CURRENT_TIMESTAMP),
    ('00000000-0000-0000-0000-000000000002'::UUID, 'Users', 'Users Management', 'User and account management', true, CURRENT_TIMESTAMP),
    ('00000000-0000-0000-0000-000000000003'::UUID, 'Analytics', 'Analytics Module', 'Advanced analytics and insights', true, CURRENT_TIMESTAMP),
    ('00000000-0000-0000-0000-000000000004'::UUID, 'Settings', 'Settings Module', 'System configuration and settings', true, CURRENT_TIMESTAMP),
    ('00000000-0000-0000-0000-000000000005'::UUID, 'Audit', 'Audit Logs', 'Audit trail and logging', true, CURRENT_TIMESTAMP)
ON CONFLICT ("Code") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "IsActive" = EXCLUDED."IsActive";

-- =====================================================
-- 6. SEED ROLES
-- =====================================================

INSERT INTO roles ("Id", "Code", "Name", "Description", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
    ('10000000-0000-0000-0000-000000000001'::UUID, 'Admin', 'Administrator', 'Full system access', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('10000000-0000-0000-0000-000000000002'::UUID, 'Manager', 'Manager', 'Department and report management', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('10000000-0000-0000-0000-000000000003'::UUID, 'Editor', 'Editor', 'Content creation and editing', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('10000000-0000-0000-0000-000000000004'::UUID, 'Viewer', 'Viewer', 'Read-only access', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT ("Code") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "Description" = EXCLUDED."Description",
    "IsActive" = EXCLUDED."IsActive",
    "UpdatedAt" = CURRENT_TIMESTAMP;

-- =====================================================
-- 7. SEED ROLE_SUBSYSTEM_PERMISSIONS
-- =====================================================

-- Permission flags (bitwise):
-- View = 1, Create = 2, Edit = 4, Delete = 8, Export = 16, Approve = 32, 
-- Execute = 64, Audit = 128, ManageUsers = 256, ManageRoles = 4096, ManagePermissions = 8192
-- Admin full access = 12799
-- Manager = 295 (View | Create | Edit | Approve | ManageUsers)
-- Editor = 7 (View | Create | Edit)
-- Viewer = 1 (View only)

INSERT INTO role_subsystem_permissions ("RoleId", "SubsystemId", "Flags", "UpdatedAt")
VALUES
    -- Admin role: full access on all subsystems (12799)
    ('10000000-0000-0000-0000-000000000001'::UUID, '00000000-0000-0000-0000-000000000001'::UUID, 12799, CURRENT_TIMESTAMP),  -- Reports
    ('10000000-0000-0000-0000-000000000001'::UUID, '00000000-0000-0000-0000-000000000002'::UUID, 12799, CURRENT_TIMESTAMP),  -- Users
    ('10000000-0000-0000-0000-000000000001'::UUID, '00000000-0000-0000-0000-000000000003'::UUID, 12799, CURRENT_TIMESTAMP),  -- Analytics
    ('10000000-0000-0000-0000-000000000001'::UUID, '00000000-0000-0000-0000-000000000004'::UUID, 12799, CURRENT_TIMESTAMP),  -- Settings
    ('10000000-0000-0000-0000-000000000001'::UUID, '00000000-0000-0000-0000-000000000005'::UUID, 12799, CURRENT_TIMESTAMP),  -- Audit

    -- Manager role: restricted access (295 = View | Create | Edit | Approve | ManageUsers)
    ('10000000-0000-0000-0000-000000000002'::UUID, '00000000-0000-0000-0000-000000000001'::UUID, 295, CURRENT_TIMESTAMP),   -- Reports
    ('10000000-0000-0000-0000-000000000002'::UUID, '00000000-0000-0000-0000-000000000002'::UUID, 295, CURRENT_TIMESTAMP),   -- Users
    ('10000000-0000-0000-0000-000000000002'::UUID, '00000000-0000-0000-0000-000000000003'::UUID, 1, CURRENT_TIMESTAMP),     -- Analytics: View only

    -- Editor role: limited access (7 = View | Create | Edit)
    ('10000000-0000-0000-0000-000000000003'::UUID, '00000000-0000-0000-0000-000000000001'::UUID, 7, CURRENT_TIMESTAMP),     -- Reports
    ('10000000-0000-0000-0000-000000000003'::UUID, '00000000-0000-0000-0000-000000000002'::UUID, 1, CURRENT_TIMESTAMP),     -- Users: View only

    -- Viewer role: read-only access (1 = View only)
    ('10000000-0000-0000-0000-000000000004'::UUID, '00000000-0000-0000-0000-000000000001'::UUID, 1, CURRENT_TIMESTAMP),     -- Reports
    ('10000000-0000-0000-0000-000000000004'::UUID, '00000000-0000-0000-0000-000000000003'::UUID, 1, CURRENT_TIMESTAMP)      -- Analytics
ON CONFLICT ("RoleId", "SubsystemId") DO UPDATE SET
    "Flags" = EXCLUDED."Flags",
    "UpdatedAt" = CURRENT_TIMESTAMP;

-- =====================================================
-- 8. VERIFICATION
-- =====================================================

SELECT 'RBAC System Setup Complete!' as Status;
SELECT COUNT(*) as Subsystems_Count FROM subsystems;
SELECT COUNT(*) as Roles_Count FROM roles;
SELECT COUNT(*) as Permissions_Count FROM role_subsystem_permissions;
