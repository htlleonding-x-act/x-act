using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using NodaTime.Serialization.SystemTextJson;
using XActBackend;
using XActBackend.Realtime;
using XActBackend.Shared;
using XActBackend.Util;

var builder = WebApplication.CreateBuilder(args);

bool isDev = builder.Environment.IsDevelopment();
var configurationManager = builder.Configuration;
var settings = builder.Services.LoadAndConfigureSettings(configurationManager);

builder.AddLogging();
builder.Services.AddApplicationServices(configurationManager, isDev);
builder.Services.AddOpenApi();
builder.Services.AddCors(settings);
builder.Services.AddRealtime(isDev);
builder.Services.AddControllers(o => { o.ModelBinderProviders.Insert(0, new NodaTimeModelBinderProvider()); })
       .AddJsonOptions(o => ConfigureJsonSerialization(o, isDev));
builder.Services.ConfigureAdditionalRouteConstraints();

var app = builder.Build();

// no https here, every production backend sits behind a reverse proxy that terminates tls

app.UseCors(Setup.CorsPolicyName);
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();
app.MapHub<GameSessionHub>("/hubs/game-session");
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.RunAsync();

return;

static void ConfigureJsonSerialization(JsonOptions options, bool isDev)
{
    JsonConfig.ConfigureJsonSerialization(options.JsonSerializerOptions, isDev);
}

// public so the integration tests can pass it to WebApplicationFactory
public sealed partial class Program { }