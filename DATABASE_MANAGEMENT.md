# Database Management Guide

This guide explains how to manage the SQLite database for the Game Auth API.

## 📋 Quick Start

### Reset Database (Recommended)

The easiest way to reset the database is using the provided script:

```bash
./reset-database.sh
```

This script will:
1. ✅ Check if `dotnet` and `dotnet-ef` are installed
2. ✅ Remove existing database files (`gameauth.db`, `.db-shm`, `.db-wal`)
3. ✅ Apply all EF Core migrations
4. ✅ Verify the database was created successfully
5. ✅ Display database information

## 🛠️ Manual Database Management

### 1. Delete Database Files

```bash
cd Rest
rm -f gameauth.db gameauth.db-shm gameauth.db-wal
```

### 2. Apply Migrations

#### Method A: Using dotnet ef (if installed)

```bash
cd Rest
dotnet ef database update
```

#### Method B: Run the Application

The application automatically applies migrations on startup:

```bash
cd Rest
dotnet run
```

The migrations will be applied before the application starts.

### 3. Create New Migration

When you make changes to your models:

```bash
cd Rest
dotnet ef migrations add YourMigrationName
```

### 4. Remove Last Migration

If you need to undo the last migration:

```bash
cd Rest
dotnet ef migrations remove
```

## 📊 Database Information

### Location
- **File**: `Rest/gameauth.db`
- **Type**: SQLite
- **Naming Convention**: snake_case

### Seeded Test Users

When `SeedData:Enabled` is `true` in `appsettings.Development.json`, the following users are created:

| Username | Email | Password | Purpose |
|----------|-------|----------|---------|
| testuser | testuser@game.com | Password123! | General testing |
| player1 | player1@game.com | Password123! | Player testing |
| admin | admin@game.com | Password123! | Admin testing |

### Database Tables

The database includes:

- **AspNetUsers** - User accounts (with indexes)
  - `idx_applicationuser_email` (unique)
  - `idx_applicationuser_username` (unique)
  - `idx_applicationuser_lastlogin`

- **refresh_tokens** - Refresh tokens (owned by users)
  - `idx_refreshtoken_token` (unique)
  - `idx_refreshtoken_expires`
  - `idx_refreshtoken_revoked`

- **AspNetRoles** - Identity roles
- **AspNetUserRoles** - User-role relationships
- **AspNetUserClaims** - User claims
- **AspNetUserLogins** - External login providers
- **AspNetUserTokens** - User tokens
- **AspNetRoleClaims** - Role claims
- **__EFMigrationsHistory** - Migration tracking

## 🔧 Troubleshooting

### dotnet-ef not found

If you get "dotnet-ef not found", install it:

```bash
dotnet tool install --global dotnet-ef
```

Then add to your PATH:

```bash
export PATH="$HOME/.dotnet/tools:$PATH"
```

### Database locked

If you get "database is locked" error:

```bash
# Stop the application
# Delete the database files
rm -f Rest/gameauth.db*

# Run the reset script
./reset-database.sh
```

### Migration pending

If you see "migration pending" warning:

```bash
cd Rest
dotnet ef database update
```

Or simply run the reset script:

```bash
./reset-database.sh
```

## 🎯 Common Scenarios

### Starting Fresh

```bash
./reset-database.sh
```

### Testing with Clean Data

```bash
./reset-database.sh
cd Rest
dotnet run
```

### Production Deployment

For production, use a proper database (PostgreSQL, SQL Server) and:

1. **Disable seeding** in `appsettings.json`:
   ```json
   "SeedData": {
     "Enabled": false
   }
   ```

2. **Apply migrations manually**:
   ```bash
   dotnet ef database update --connection "YourConnectionString"
   ```

3. **Use environment variables** for connection strings:
   ```bash
   export ConnectionStrings__DefaultConnection="YourConnectionString"
   ```

## 📝 Migration Workflow

### Development Workflow

1. Make changes to models (e.g., `ApplicationUser.cs`)
2. Create migration:
   ```bash
   dotnet ef migrations add AddNewFeature
   ```
3. Review the migration file in `Migrations/`
4. Apply migration:
   ```bash
   dotnet ef database update
   ```
   Or run the app (auto-applies)

### Team Workflow

1. Pull latest code
2. Reset database to apply new migrations:
   ```bash
   ./reset-database.sh
   ```
3. Start developing

## 🔍 Inspecting the Database

### Using .NET

The application logs all SQL queries in Development mode. Check the console output.

### Using sqlite3 (if installed)

```bash
# Open database
sqlite3 Rest/gameauth.db

# List tables
.tables

# View schema
.schema AspNetUsers

# Query users
SELECT user_name, email FROM AspNetUsers;

# Exit
.quit
```

### Using DB Browser for SQLite

Download: https://sqlitebrowser.org/

1. Open `Rest/gameauth.db`
2. Browse tables
3. Execute SQL queries

## 🚀 Advanced Operations

### Backup Database

```bash
cp Rest/gameauth.db Rest/gameauth.db.backup
```

### Restore Database

```bash
cp Rest/gameauth.db.backup Rest/gameauth.db
```

### Export Data

```bash
sqlite3 Rest/gameauth.db .dump > backup.sql
```

### Import Data

```bash
sqlite3 Rest/gameauth.db < backup.sql
```

## 🔐 Security Notes

- **Never commit** `gameauth.db` to version control (it's in `.gitignore`)
- **Never use** `Password123!` in production
- **Always use** environment variables or Azure Key Vault for production connection strings
- **Enable SSL/TLS** for production database connections
- **Regularly backup** production databases

## 📚 Additional Resources

- [EF Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
- [ASP.NET Core Data Protection](https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/)

---

**Need Help?** Check the logs in the console or the `Logs/` directory for detailed error messages.
