using XAct.Core;
using XAct.Core.GameSessions;
using XAct.Core.GeofencePoints;
using XAct.Core.LocationLogs;
using XAct.Core.PowerUpUsages;
using XAct.Core.TeamMembers;
using XAct.Core.Teams;
using XAct.Core.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.RegisterServices();
builder.Services.ConfigureServices(builder.Environment.IsDevelopment());
builder.Services.ConfigureCors();

var app = builder.Build();

app.UseCors(Const.CorsPolicyName);

app.MapGameSessionEndpoint();
app.MapUserEndpoint();
app.MapGeofencePointEndpoint();
app.MapTeamEndpoint();
app.MapTeamMemberEndpoint();
app.MapLocationLogEndpoint();  
app.MapPowerUpUsageEndpoint();

await app.RunAsync();
