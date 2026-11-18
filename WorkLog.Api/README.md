# WorkLog.Api

This ASP.NET Core API targets .NET 10 and registers its data-access services from `WorkLog.Infrastructure`.

Before starting the API, configure a PostgreSQL connection string using the `ConnectionStrings:WorkLog` configuration key. For example, in PowerShell:

```powershell
$env:ConnectionStrings__WorkLog = "Host=localhost;Database=worklog;Username=<user>;Password=<password>"
dotnet run --project WorkLog.Api
```

Keep real credentials in user secrets or environment variables; do not commit them. The OpenAPI document is exposed at `/openapi/v1.json` in Development.
