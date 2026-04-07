using AccessControl.Api.Endpoints;
using AccessControl.Api.Hubs;
using AccessControl.Api.Infrastructure;
using AccessControl.Application;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Infrastructure;
using AccessControl.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Application layer (MediatR, FluentValidation)
builder.Services.AddApplication();

// Infrastructure layer (DbContext, Identity, JWT, PostgreSQL)
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSignalR();

// Registered here (not in Infrastructure DI) because AccessNotificationService
// depends on IHubContext<AccessControlHub> which lives in the Api layer.
builder.Services.AddScoped<IAccessNotificationService, AccessNotificationService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorWasmPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", "Authorization", "x-signalr-user-agent", "x-requested-with")
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

// Auto-migration (development only)
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
    await db.Database.MigrateAsync();
}

// Seed default admin account (idempotent)
{
    await using var scope = app.Services.CreateAsyncScope();
    var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
    await seeder.SeedAsync();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

// Scalar UI (development only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("BlazorWasmPolicy");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Health
app.MapHealthChecks("/health").AllowAnonymous().WithTags("Health");

// SignalR
app.MapHub<AccessControlHub>("/hubs/access-control");

// Endpoints
app.MapAuthEndpoints();
app.MapDeviceEndpoints();
app.MapCardEndpoints();
app.MapZoneEndpoints();
app.MapAccessLogEndpoints();
app.MapCardholderEndpoints();
app.MapAccessProfileEndpoints();

app.Run();
