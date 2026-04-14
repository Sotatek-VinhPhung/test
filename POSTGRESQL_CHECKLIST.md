# 🐘 PostgreSQL Migration - Printable Checklist

## ✅ Configuration Phase (Already Done!)

```
[✓] Connection string updated in appsettings.json
[✓] DbContext changed to UseNpgsql()
[✓] EF Core migration created
[✓] Build verified (0 errors)
[✓] Documentation created
[✓] Setup scripts provided
```

---

## 📋 Pre-Migration Checklist

### Requirements Met?
```
[ ] PostgreSQL installed on system
[ ] PostgreSQL service running or Docker ready
[ ] Port 5432 available
[ ] Admin credentials available
[ ] Network access to localhost:5432
```

### Environment Ready?
```
[ ] .NET 8.0 SDK installed
[ ] Visual Studio / VS Code ready
[ ] Terminal/PowerShell available
[ ] Git repository up to date
[ ] Project builds successfully
```

---

## 🚀 Migration Steps (Do These Now!)

### Step 1: Start PostgreSQL ⏱️ (1 minute)

**Copy one of these commands:**

**Option A - Docker (Easiest):**
```
docker run --name postgres-rbac -e POSTGRES_PASSWORD=123456 -p 5432:5432 -d postgres:latest
```

**Option B - Local Installation:**
- Windows: Open Services → PostgreSQL → Start
- Mac: brew services start postgresql
- Linux: sudo systemctl start postgresql

**Verification:**
```
pg_isready -h localhost -p 5432
```

Status: ____________________

### Step 2: Create Database ⏱️ (30 seconds)

**Copy this command:**
```
psql -h localhost -U postgres -c "CREATE DATABASE rbac_db;"
```

**Or use setup script:**
```
.\setup-postgres.ps1
```

Status: ____________________

### Step 3: Apply Migration ⏱️ (1 minute)

**Copy these commands:**
```
cd C:\test
dotnet ef database update -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api
```

**Expected output:**
```
Applying migration '20260414072821_InitialCreatePostgres'.
Done.
```

Status: ____________________

### Step 4: Verify Tables ⏱️ (30 seconds)

**Copy this command:**
```
psql -h localhost -U postgres -d rbac_db -c "\dt"
```

**Should see these tables:**
- [ ] Roles
- [ ] Subsystems
- [ ] UserRoles
- [ ] RoleSubsystemPermissions
- [ ] Users
- [ ] UserPermissionOverrides
- [ ] RolePermissions

Status: ____________________

### Step 5: Run Application ⏱️ (1 minute)

**Copy this command:**
```
cd C:\test
dotnet run --project src\CleanArchitecture.Api
```

**Expected output:**
```
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```

Status: ____________________

### Step 6: Test with Swagger ⏱️ (2 minutes)

**Open browser:**
```
http://localhost:5000/swagger
```

**Try an endpoint:**
- [ ] Click "Try it out"
- [ ] Click "Execute"
- [ ] Should get 200 or auth error (not DB error)

Status: ____________________

---

## ✅ Post-Migration Verification

### Database Connected?
```
[ ] No connection errors in console
[ ] No timeout errors
[ ] Database queries working
[ ] All tables present
```

### API Working?
```
[ ] API starts without errors
[ ] Swagger UI loads
[ ] Endpoints respond
[ ] No database errors in logs
```

### Data Integrity?
```
[ ] Existing data preserved (if any)
[ ] New records can be created
[ ] Queries return correct data
[ ] Permissions system working
```

---

## 🔧 Connection String Reference

```
Server=localhost
Port=5432
Database=rbac_db
User Id=postgres
Password=123456
```

**Full string:**
```
Server=localhost;Port=5432;Database=rbac_db;User Id=postgres;Password=123456;
```

---

## 📚 Documentation Files Available

```
[ ] POSTGRESQL_INDEX.md - Start here for navigation
[ ] POSTGRESQL_QUICK_START.md - Quick reference
[ ] MIGRATION_SUMMARY.md - Overview
[ ] MIGRATION_VISUAL_GUIDE.md - Diagrams
[ ] POSTGRESQL_SETUP_COMPLETE.md - Detailed guide
[ ] POSTGRESQL_MIGRATION_GUIDE.md - Comprehensive
```

---

## 🐛 If Something Goes Wrong...

### Can't connect to PostgreSQL?
```
1. Check: pg_isready -h localhost -p 5432
2. Start PostgreSQL (see Step 1 above)
3. Check port 5432 is available
```

### Database doesn't exist?
```
1. Run: psql -h localhost -U postgres -c "CREATE DATABASE rbac_db;"
2. Verify: psql -h localhost -U postgres -l
```

### Migration fails?
```
1. Run with verbose: dotnet ef database update --verbose
2. Check error messages in output
3. See POSTGRESQL_MIGRATION_GUIDE.md for solutions
```

### Application won't start?
```
1. Check build: dotnet build
2. Check connection string in appsettings.json
3. Check PostgreSQL is running
```

---

## 📊 Time Estimates

| Step | Task | Time |
|------|------|------|
| 1 | Start PostgreSQL | 1 min |
| 2 | Create database | 30 sec |
| 3 | Apply migration | 1 min |
| 4 | Verify tables | 30 sec |
| 5 | Run application | 1 min |
| 6 | Test with Swagger | 2 min |
| | **Total** | **~6 minutes** |

---

## ✨ Success Indicators

When complete, you should see:

✅ All steps completed without errors
✅ Migration applied successfully
✅ All tables present in PostgreSQL
✅ Application starts without errors
✅ Swagger loads at http://localhost:5000/swagger
✅ API endpoints respond correctly

---

## 🎯 Final Verification

```
VERIFY: [ ] Configuration complete
        [ ] PostgreSQL running
        [ ] Database created
        [ ] Migration applied
        [ ] Tables created
        [ ] Application started
        [ ] Swagger loading
        [ ] API endpoints working

Status: ✅ All Complete!
```

---

## 📞 Support

- **Quick questions?** → POSTGRESQL_QUICK_START.md
- **Setup help?** → POSTGRESQL_SETUP_COMPLETE.md
- **Technical details?** → POSTGRESQL_MIGRATION_GUIDE.md
- **Visual explanation?** → MIGRATION_VISUAL_GUIDE.md

---

## 🎉 You're Done!

Once all steps completed:
```
✅ PostgreSQL Migration Complete
✅ Application Running Successfully
✅ Database Fully Connected
✅ Ready for Development/Testing
```

---

**Print this checklist and mark off each step as you complete it! 📋**

**Last Updated**: 2024-04-14
**Status**: Ready for Migration
