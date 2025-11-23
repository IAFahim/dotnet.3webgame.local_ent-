# ⚡ Quick Start Guide

## Start the Application

```bash
cd GameStore.AppHost
dotnet run
```

Or use Aspire CLI:
```bash
cd GameStore.AppHost
aspire run
```

## What Happens?

1. **Aspire Dashboard** opens at https://localhost:17211
2. **PostgreSQL** container starts automatically
3. **pgAdmin** container starts automatically  
4. **Rest API** starts and connects to PostgreSQL
5. **Database migrations** run automatically

## View Everything

Open the Aspire Dashboard (URL shown in console) to see:
- All running resources
- Real-time logs
- Connection strings
- Health status
- Performance metrics
- Distributed traces

## Current Running Status

Check what's running:
```bash
docker ps
```

You should see:
- PostgreSQL container
- pgAdmin container

## Stop Everything

Press `Ctrl+C` in the terminal running Aspire.

All containers will stop automatically!

## That's It! 🎉

No need to:
- ❌ Manually start PostgreSQL
- ❌ Configure connection strings
- ❌ Run migrations manually
- ❌ Set up logging/monitoring

Everything is automated with Aspire!
