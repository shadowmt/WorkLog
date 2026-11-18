using WorkLog.Api.Endpoints;
using WorkLog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidation();

var connectionString = builder.Configuration.GetConnectionString("WorkLog");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "The 'WorkLog' connection string is missing. Configure ConnectionStrings:WorkLog before starting the API.");
}

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapProjectEndpoints();

app.UseHttpsRedirection();

app.Run();

public partial class Program
{
}
