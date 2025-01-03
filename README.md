## Notes:
1. Original template taken from:
https://github.com/msuddaby/ASPNetCoreJWTAuthTemplate
2. Docker Desktop v.4.36

## Manual for deploy:
1. Open Docker Desktop.
2. Open solution in Visual Studio.
3. Open solution in terminal.
4. If there is nessesary to run in docker application and database, use command:

**docker-compose up -d**

If need to run only database:

**docker-compose up -d db**

5. Add migrations, use command:

**dotnet ef database update**

6. Run solution in "https" mode.