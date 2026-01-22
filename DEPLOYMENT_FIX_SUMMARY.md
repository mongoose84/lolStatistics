# Production Deployment Fix Summary

## Problem
Your production server was not connecting to the database and SMTP was not configured.

## Root Causes Found

### 1. Missing `appsettings.Production.json` in Deployment
The GitHub Actions workflow generated the file but didn't copy it to the publish output directory.

**Fix:** Updated `.github/workflows/ci-server.yml` to explicitly copy `appsettings.Production.json` to the publish output.

### 2. ⚠️ **CRITICAL**: `ASPNETCORE_ENVIRONMENT` Not Set to Production
The production server didn't have the `ASPNETCORE_ENVIRONMENT` environment variable set, so it defaulted to Development mode.

**Impact:**
- Server looked for `Database_test` instead of `Database_production`
- Your `appsettings.Production.json` has the connection string under `Database_production`
- Result: Connection string not found!

**Evidence from logs:**
```
[Secrets.Initialize] Environment: 
[Secrets.Initialize] IsProduction: False
[Secrets.GetDatabaseConnectionString] Looking for: Database_test
  - GetConnectionString: NOT_SET
```

**Fix:** Updated `server/web.config` to set `ASPNETCORE_ENVIRONMENT=Production` via environment variables.

## Files Changed

1. **`.github/workflows/ci-server.yml`**
   - Added step to copy `appsettings.Production.json` to publish output
   - Added verification to ensure file exists before deployment
   - Simplified "bring site back online" logic (delete instead of move)
   - Added debug logging configuration

2. **`server/web.config`**
   - Added `<environmentVariables>` section
   - Set `ASPNETCORE_ENVIRONMENT=Production`

3. **`server/CI-Support/web.config`**
   - Added `<environmentVariables>` section
   - Set `ASPNETCORE_ENVIRONMENT=Production`

4. **`server/Application/Endpoints/Diagnostics/DiagnosticsEndpoint.cs`**
   - Enhanced diagnostics to show configuration sources
   - Added debug information to help troubleshoot configuration issues

5. **`server/Infrastructure/Configuration/Secrets.cs`**
   - Added detailed debug logging
   - Shows which configuration sources are checked and what values are found

## Next Steps

### 1. Commit and Push Changes
```bash
git add .
git commit -m "Fix: Set ASPNETCORE_ENVIRONMENT=Production and ensure appsettings.Production.json is deployed"
git push origin main
```

### 2. Monitor Deployment
- Watch the GitHub Actions workflow
- Verify the "Copy appsettings.Production.json" step succeeds
- Check that the file is present in the publish output

### 3. Verify the Fix
After deployment completes, call the diagnostics endpoint:
```bash
curl https://api.mongoose.gg/api/v2/diagnostics
```

**Expected result:**
```json
{
  "configuration": {
    "apiKeyConfigured": true,
    "databaseConfigured": true,
    "smtpConfigured": true,
    "emailEncryptionKeyConfigured": true,
    "allConfigured": true
  }
}
```

### 4. Check Server Logs
Look for these lines in the logs (should be in `logs/stdout-*.log`):
```
[Secrets.Initialize] Environment: Production
[Secrets.Initialize] IsProduction: True
[Secrets.GetDatabaseConnectionString] Looking for: Database_production
  - GetConnectionString: SET (130 chars)
  - Final result: SET (130 chars)
[Secrets.Initialize] Database configured: True (Production)
```

## Why This Happened

1. **Missing Environment Variable**: The production server hosting environment (IIS/Windows) didn't have `ASPNETCORE_ENVIRONMENT` set, so ASP.NET Core defaulted to Development mode.

2. **Configuration Key Mismatch**: The code looks for different database keys based on environment:
   - Production → `Database_production`
   - Development/Test → `Database_test`

3. **File Not Deployed**: The `appsettings.Production.json` file wasn't being copied to the publish output, so even if the environment was correct, the config file wouldn't have been there.

## Prevention

- ✅ `web.config` now explicitly sets `ASPNETCORE_ENVIRONMENT=Production`
- ✅ GitHub Actions workflow now verifies `appsettings.Production.json` is in publish output
- ✅ Enhanced diagnostics endpoint shows configuration sources for easier troubleshooting
- ✅ Debug logging helps identify configuration issues during startup

## Testing Checklist

After deployment:
- [ ] `/api/v2/diagnostics` shows `allConfigured: true`
- [ ] Server logs show `Environment: Production`
- [ ] Server logs show `Database configured: True (Production)`
- [ ] Database queries work (test any endpoint that uses the database)
- [ ] Email verification works (test user registration)
- [ ] No errors in server logs about missing configuration

