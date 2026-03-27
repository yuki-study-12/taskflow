using TaskFlow.Application;
using TaskFlow.Infrastructure;
using TaskFlow.WebAPI.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// Application + Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Auth endpoints
app.MapAuthEndpoints();

app.Run();
