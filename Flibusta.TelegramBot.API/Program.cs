using Flibusta.TelegramBot.Domain.Abstractions;
using Flibusta.TelegramBot.FlibustaApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

builder.Services.AddFlibustaApi();

var app = builder.Build();

app.MapControllers();

app.Run();
