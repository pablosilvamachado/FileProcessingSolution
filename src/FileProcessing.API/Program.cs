using System.Text;
using FileProcessing.Application.Interfaces;
using FileProcessing.Infrastructure.Persistence;
using FileProcessing.Infrastructure.Repositories;
using FileProcessing.Infrastructure.Storage;
using FileProcessing.Api.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// Serilog
// ---------------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ---------------------------
// DbContext (Postgres)
// ---------------------------
// Ensure your infra project defines FileProcessingDbContext with migrations assembly if needed
builder.Services.AddDbContext<FileProcessingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("FileProcessing.Infrastructure")));

// ---------------------------
// DI - Repositories, Storage, Producer wrapper
// ---------------------------
builder.Services.AddScoped<IFileRecordRepository, FileRecordRepository>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IMessageProducerService, MessageProducerService>();

// ---------------------------
// MassTransit - Producer (RabbitMQ)
// ---------------------------
builder.Services.AddMassTransit(x =>
{
    // no consumers here (API is producer)
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
        var user = builder.Configuration["RabbitMQ:Username"] ?? builder.Configuration["RabbitMQ:User"] ?? "guest";
        var pass = builder.Configuration["RabbitMQ:Password"] ?? builder.Configuration["RabbitMQ:Pass"] ?? "guest";

        // configure host (use slash "/" vhost)
        cfg.Host(host, "/", h =>
        {
            h.Username(user);
            h.Password(pass);
        });
    });
});

// ---------------------------
// Authentication (JWT)
// ---------------------------
// Ensure Jwt:Key exists in configuration (appsettings)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "change-this-secret-in-dev";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });


builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default") ?? string.Empty, name: "postgres")
    .AddRabbitMQ("amqp://guest:guest@rabbitmq:5672/", name: "rabbitmq");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------------------------
// Build app
// ---------------------------
var app = builder.Build();

// Apply Migrations automatically in development (optional)
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FileProcessingDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Log.Logger?.Warning(ex, "Could not apply migrations automatically.");
    }
}

// ---------------------------
// Middleware pipeline
// ---------------------------
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Finalize
app.Run();
