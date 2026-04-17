using TaskFlow.Application;
using TaskFlow.Infrastructure;
using TaskFlow.WebAPI.Endpoints;
using TaskFlow.WebAPI.Middleware;
using TaskFlow.WebAPI.OpenApi;

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

app.Run();
