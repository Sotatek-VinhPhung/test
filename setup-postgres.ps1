# PowerShell Setup Script for PostgreSQL RBAC Database
# Run as administrator

Write-Host "🐘 PostgreSQL Setup Script (Windows PowerShell)" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$dbHost = "localhost"
$dbPort = "5432"
$dbName = "rbac_db"
$dbUser = "postgres"

Write-Host "📋 Configuration:" -ForegroundColor Yellow
Write-Host "  Host: $dbHost"
Write-Host "  Port: $dbPort"
Write-Host "  Database: $dbName"
Write-Host "  User: $dbUser"
Write-Host ""

# Step 1: Check if psql is available
Write-Host "✓ Step 1: Checking PostgreSQL installation..." -ForegroundColor Cyan
try {
    $psqlVersion = psql --version 2>$null
    if ($psqlVersion) {
        Write-Host "  ✅ PostgreSQL CLI found: $psqlVersion" -ForegroundColor Green
    } else {
        Write-Host "  ❌ PostgreSQL CLI not found in PATH" -ForegroundColor Red
        Write-Host "  Please add PostgreSQL bin directory to PATH"
        exit 1
    }
} catch {
    Write-Host "  ❌ Error checking PostgreSQL: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Test database connection
Write-Host ""
Write-Host "✓ Step 2: Testing database connection..." -ForegroundColor Cyan
$env:PGPASSWORD = $env:DB_PASSWORD
try {
    $result = psql -h $dbHost -U $dbUser -tc "SELECT 1" 2>$null
    if ($result -match "1") {
        Write-Host "  ✅ Connected to PostgreSQL successfully" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  Connection test returned unexpected result" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ❌ Cannot connect to PostgreSQL" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    Write-Host "  Please ensure:"
    Write-Host "    1. PostgreSQL service is running"
    Write-Host "    2. Port 5432 is accessible"
    Write-Host "    3. Username 'postgres' is correct"
    exit 1
}

# Step 3: Create database if not exists
Write-Host ""
Write-Host "✓ Step 3: Creating database (if not exists)..." -ForegroundColor Cyan
try {
    $dbExists = psql -h $dbHost -U $dbUser -tc "SELECT 1 FROM pg_database WHERE datname = '$dbName'" 2>$null
    if ($dbExists -match "1") {
        Write-Host "  ℹ️  Database '$dbName' already exists" -ForegroundColor Gray
    } else {
        Write-Host "  Creating new database '$dbName'..."
        psql -h $dbHost -U $dbUser -c "CREATE DATABASE $dbName;" 2>$null
        Write-Host "  ✅ Database '$dbName' created successfully" -ForegroundColor Green
    }
} catch {
    Write-Host "  ❌ Error creating database: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Display connection information
Write-Host ""
Write-Host "✓ Step 4: Connection Information" -ForegroundColor Cyan
Write-Host "  Connection String (for appsettings.json):" -ForegroundColor White
Write-Host "  Server=localhost;Port=5432;Database=$dbName;User Id=$dbUser;Password=***;" -ForegroundColor Yellow
Write-Host ""

# Step 5: Display next steps
Write-Host "✓ Step 5: Next Steps" -ForegroundColor Cyan
Write-Host "  1. Update appsettings.json with your password"
Write-Host "  2. Run migrations:" -ForegroundColor White
Write-Host "     cd C:\test" -ForegroundColor Yellow
Write-Host "     dotnet ef database update -p src\CleanArchitecture.Infrastructure -s src\CleanArchitecture.Api" -ForegroundColor Yellow
Write-Host ""

# Step 6: Optional - Display created tables after migration
Write-Host "✓ Step 6: Verify Tables (after migration)" -ForegroundColor Cyan
Write-Host "  To check created tables, run:" -ForegroundColor White
Write-Host "     psql -h $dbHost -U $dbUser -d $dbName -c '\dt'" -ForegroundColor Yellow
Write-Host ""

Write-Host "✅ Setup complete!" -ForegroundColor Green
Write-Host ""

# Cleanup
Remove-Variable -Name "env:PGPASSWORD" -ErrorAction SilentlyContinue
