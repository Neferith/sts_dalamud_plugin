using Sts.Api.Endpoints;
using Sts.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

// Service qui lit et sert le data.json
builder.Services.AddSingleton<DataService>();

var app = builder.Build();

// Endpoints
app.MapDataEndpoints();

app.Run();
