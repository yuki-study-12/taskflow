using TaskFlow.Application;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Infrastructure;
using TaskFlow.WebAPI.Endpoints;
using TaskFlow.WebAPI.Hubs;
using TaskFlow.WebAPI.Middleware;
using TaskFlow.WebAPI.OpenApi;
using TaskFlow.WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger with Bearer token support
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// ProblemDetails + exception handler
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Application + Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<IBoardNotificationService, BoardHubNotificationService>();
builder.Services.AddScoped<INotificationHubService, NotificationHubService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Auth endpoints
app.MapAuthEndpoints();

// Project endpoints
app.MapProjectEndpoints();

// Board endpoints
app.MapBoardEndpoints();

// Notification endpoints
app.MapNotificationEndpoints();

// SignalR hubs
app.MapHub<BoardHub>("/hubs/board");
app.MapHub<NotificationHub>("/hubs/notification");

app.Run();
