using WorkLog.Application;
using WorkLog.Api.Endpoints;
using WorkLog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidation();

builder.Services.AddSingleton<TimeProvider>(
    TimeProvider.System);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// 接続文字列は DbContext の解決時に読み出すため、設定不備はここで起動時に検出する。
app.Configuration.GetRequiredWorkLogConnectionString();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapProjectEndpoints();
app.MapWorkLogEntryEndpoints();

app.UseHttpsRedirection();

app.Run();

public partial class Program
{
}
