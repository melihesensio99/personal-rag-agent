using TelegramAi.Backend.Api;
using TelegramAi.Backend.Application;
using TelegramAi.Backend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapApiEndpoints();

app.Run();

public partial class Program;
