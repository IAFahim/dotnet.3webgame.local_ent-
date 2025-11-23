#!/bin/bash

#############################################
# Database Reset and Migration Script
# 
# This script will:
# 1. Check if dotnet ef tools are installed
# 2. Remove existing database files
# 3. Apply all migrations
# 4. Seed the database (if configured)
#############################################

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="${SCRIPT_DIR}/Rest"
DB_FILE="${PROJECT_DIR}/gameauth.db"
DB_SHM_FILE="${PROJECT_DIR}/gameauth.db-shm"
DB_WAL_FILE="${PROJECT_DIR}/gameauth.db-wal"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}   Database Reset & Migration Script${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Step 1: Check if dotnet is installed
echo -e "${YELLOW}[1/6]${NC} Checking dotnet installation..."
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}✗ dotnet is not installed or not in PATH${NC}"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo -e "${GREEN}✓ dotnet ${DOTNET_VERSION} is installed${NC}"
echo ""

# Step 2: Check if dotnet ef tools are installed
echo -e "${YELLOW}[2/6]${NC} Checking dotnet ef tools..."

if ! dotnet ef &> /dev/null; then
    echo -e "${RED}✗ dotnet ef tools are not installed${NC}"
    echo -e "${YELLOW}Installing dotnet ef tools...${NC}"
    
    # Try to install globally
    if dotnet tool install --global dotnet-ef; then
        echo -e "${GREEN}✓ dotnet ef tools installed successfully${NC}"
        
        # Update PATH for current session
        export PATH="$HOME/.dotnet/tools:$PATH"
        
        # Verify installation
        if ! dotnet ef &> /dev/null; then
            echo -e "${RED}✗ Failed to install dotnet ef tools${NC}"
            echo -e "${YELLOW}Please install manually: dotnet tool install --global dotnet-ef${NC}"
            exit 1
        fi
    else
        echo -e "${YELLOW}Tool might already be installed, trying to update...${NC}"
        dotnet tool update --global dotnet-ef || true
        export PATH="$HOME/.dotnet/tools:$PATH"
    fi
else
    echo -e "${GREEN}✓ dotnet ef tools are available${NC}"
    
    # Show version
    EF_VERSION=$(dotnet ef --version | head -n 1 || echo "Unknown")
    echo -e "  Version: ${EF_VERSION}"
fi
echo ""

# Step 3: Navigate to project directory
echo -e "${YELLOW}[3/6]${NC} Navigating to project directory..."
if [ ! -d "$PROJECT_DIR" ]; then
    echo -e "${RED}✗ Project directory not found: ${PROJECT_DIR}${NC}"
    exit 1
fi

cd "$PROJECT_DIR"
echo -e "${GREEN}✓ Current directory: $(pwd)${NC}"
echo ""

# Step 4: Remove existing database files
echo -e "${YELLOW}[4/6]${NC} Removing existing database files..."

REMOVED_COUNT=0

if [ -f "$DB_FILE" ]; then
    rm -f "$DB_FILE"
    echo -e "${GREEN}✓ Removed: gameauth.db${NC}"
    ((REMOVED_COUNT++))
fi

if [ -f "$DB_SHM_FILE" ]; then
    rm -f "$DB_SHM_FILE"
    echo -e "${GREEN}✓ Removed: gameauth.db-shm${NC}"
    ((REMOVED_COUNT++))
fi

if [ -f "$DB_WAL_FILE" ]; then
    rm -f "$DB_WAL_FILE"
    echo -e "${GREEN}✓ Removed: gameauth.db-wal${NC}"
    ((REMOVED_COUNT++))
fi

if [ $REMOVED_COUNT -eq 0 ]; then
    echo -e "${BLUE}ℹ No existing database files found${NC}"
else
    echo -e "${GREEN}✓ Removed ${REMOVED_COUNT} database file(s)${NC}"
fi
echo ""

# Step 5: Apply migrations
echo -e "${YELLOW}[5/6]${NC} Applying EF Core migrations..."

# Try different methods to apply migrations
MIGRATION_SUCCESS=false

# Method 1: Try dotnet ef if available
if command -v dotnet-ef &> /dev/null || [ -f "$HOME/.dotnet/tools/dotnet-ef" ]; then
    echo -e "${BLUE}Method 1: Using dotnet ef database update${NC}"
    export PATH="$HOME/.dotnet/tools:$PATH"
    
    if dotnet ef database update --no-build 2>/dev/null; then
        echo -e "${GREEN}✓ Migrations applied successfully${NC}"
        MIGRATION_SUCCESS=true
    elif dotnet ef database update 2>/dev/null; then
        echo -e "${GREEN}✓ Migrations applied successfully (with rebuild)${NC}"
        MIGRATION_SUCCESS=true
    fi
fi

# Method 2: Run application once (migrations run on startup)
if [ "$MIGRATION_SUCCESS" = false ]; then
    echo -e "${BLUE}Method 2: Running application to trigger migrations${NC}"
    echo -e "${YELLOW}The application will apply migrations on startup and then exit...${NC}"
    
    # Build first
    if dotnet build --configuration Release > /dev/null 2>&1; then
        # Run app for a few seconds to trigger migration
        timeout 10s dotnet run --no-build --configuration Release > /dev/null 2>&1 || true
        
        # Check if database was created
        if [ -f "$DB_FILE" ]; then
            echo -e "${GREEN}✓ Migrations applied via application startup${NC}"
            MIGRATION_SUCCESS=true
        fi
    fi
fi

# Method 3: Manual migration using dotnet run
if [ "$MIGRATION_SUCCESS" = false ]; then
    echo -e "${BLUE}Method 3: Starting application (press Ctrl+C after migrations complete)${NC}"
    echo -e "${YELLOW}Watch for the message: 'Application started. Press Ctrl+C to shut down.'${NC}"
    echo ""
    
    dotnet run --no-build &
    APP_PID=$!
    
    # Wait for database file to be created (max 30 seconds)
    for i in {1..30}; do
        if [ -f "$DB_FILE" ]; then
            echo ""
            echo -e "${GREEN}✓ Database created, migrations applied${NC}"
            kill $APP_PID 2>/dev/null || true
            wait $APP_PID 2>/dev/null || true
            MIGRATION_SUCCESS=true
            break
        fi
        sleep 1
    done
    
    if [ "$MIGRATION_SUCCESS" = false ]; then
        kill $APP_PID 2>/dev/null || true
        wait $APP_PID 2>/dev/null || true
    fi
fi

if [ "$MIGRATION_SUCCESS" = false ]; then
    echo -e "${RED}✗ All migration methods failed${NC}"
    echo -e "${YELLOW}Please run the application manually: dotnet run${NC}"
    exit 1
fi

echo ""

# Step 6: Verify database creation
echo -e "${YELLOW}[6/6]${NC} Verifying database creation..."

if [ -f "$DB_FILE" ]; then
    DB_SIZE=$(du -h "$DB_FILE" | cut -f1)
    echo -e "${GREEN}✓ Database created successfully${NC}"
    echo -e "  Location: ${DB_FILE}"
    echo -e "  Size: ${DB_SIZE}"
    
    # Show tables if sqlite3 is available
    if command -v sqlite3 &> /dev/null; then
        echo ""
        echo -e "${BLUE}Database Tables:${NC}"
        sqlite3 "$DB_FILE" ".tables" | tr ' ' '\n' | grep -v '^$' | sort | while read table; do
            echo -e "  - ${table}"
        done
    fi
else
    echo -e "${RED}✗ Database file not found after migration${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}========================================${NC}"
echo -e "${GREEN}✓ Database reset completed successfully!${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo -e "  1. Run the application: ${BLUE}dotnet run${NC}"
echo -e "  2. The database will be seeded automatically on first run (if configured)"
echo -e "  3. Access API docs at: ${BLUE}http://localhost:5083/scalar/v1${NC}"
echo ""
