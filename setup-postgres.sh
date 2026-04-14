#!/bin/bash
# PostgreSQL Setup Script for RBAC Database

echo "🐘 PostgreSQL Setup Script"
echo "=========================="

# Configuration
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="rbac_db"
DB_USER="postgres"

echo ""
echo "📋 Configuration:"
echo "  Host: $DB_HOST"
echo "  Port: $DB_PORT"
echo "  Database: $DB_NAME"
echo "  User: $DB_USER"
echo ""

# Step 1: Check PostgreSQL connection
echo "✓ Step 1: Checking PostgreSQL connection..."
psql -h $DB_HOST -U $DB_USER -tc "SELECT 1" > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "  ✅ PostgreSQL is running"
else
    echo "  ❌ Cannot connect to PostgreSQL"
    echo "  Please ensure PostgreSQL is running and accessible"
    exit 1
fi

# Step 2: Create database if not exists
echo ""
echo "✓ Step 2: Creating database (if not exists)..."
psql -h $DB_HOST -U $DB_USER -tc "SELECT 1 FROM pg_database WHERE datname = '$DB_NAME'" | grep -q 1 || psql -h $DB_HOST -U $DB_USER -c "CREATE DATABASE $DB_NAME;"
if [ $? -eq 0 ]; then
    echo "  ✅ Database '$DB_NAME' ready"
else
    echo "  ❌ Failed to create database"
    exit 1
fi

# Step 3: Display connection string
echo ""
echo "✓ Step 3: Connection information"
echo "  Connection String:"
echo "  Server=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;User Id=$DB_USER;Password=***;"
echo ""

# Step 4: Apply migrations
echo "✓ Step 4: Ready to apply migrations"
echo "  Run the following command:"
echo "  cd /path/to/project"
echo "  dotnet ef database update -p src\\CleanArchitecture.Infrastructure -s src\\CleanArchitecture.Api"
echo ""

echo "✅ Setup complete!"
