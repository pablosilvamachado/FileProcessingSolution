using FileProcessing.Api.Services;
using FileProcessing.API.Configurations;
using FileProcessing.API.Services;
using FileProcessing.Application.Interfaces;
using FileProcessing.Infrastructure.Health;
using FileProcessing.Infrastructure.Persistence;
using FileProcessing.Infrastructure.Repositories;
using FileProcessing.Infrastructure.Storage;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// Serilog - Logs estruturados
// ---------------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FileProcessingAPI")
    .CreateLogger();

builder.Host.UseSerilog();

// ---------------------------
// Services
// ---------------------------
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerWithJwt(); 


builder.Services.AddDbContext<FileProcessingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFileRecordRepository, FileRecordRepository>();
builder.Services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IMessageProducerService, MessageProducerService>();

// ---------------------------
// MassTransit - RabbitMQ
// ---------------------------
var rabbit = builder.Configuration.GetSection("RabbitMQ");

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbit["Host"], "/", h =>
        {
            h.Username(rabbit["Username"]);
            h.Password(rabbit["Password"]);
        });

        cfg.PrefetchCount = 16;
    });
});

builder.Services.AddMassTransitHostedService();

// ---------------------------
// JWT - POC
// ---------------------------
builder.Services.Configure<TokenOptions>(builder.Configuration.GetSection("Jwt"));
var tokenOptions = builder.Configuration.GetSection("Jwt").Get<TokenOptions>();
var key = Encoding.UTF8.GetBytes(tokenOptions.Key);

builder.Services.AddSingleton<ITokenService, TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = tokenOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = tokenOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ---------------------------
// HealthChecks
// ---------------------------
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres")
    .AddRabbitMQ(
        $"amqp://{rabbit["Username"]}:{rabbit["Password"]}@{rabbit["Host"]}:{rabbit["Port"]}/",
        name: "rabbitmq")
    .AddCheck<StorageHealthCheck>("storage");

// ---------------------------
// CORS
// ---------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ---------------------------
// Build app
// ---------------------------
var app = builder.Build();

//---------------------------
// Automatic migrations
// ---------------------------
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FileProcessingDbContext>();
    db.Database.Migrate();
}
catch (Exception ex)
{
    Log.Warning(ex, "Automatic migration failed.");
}

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", context.TraceIdentifier))
    {
        await next();
    }
});

app.UseAuthentication();
app.UseAuthorization();

// ---------------------------
// Controllers + Health
// ---------------------------
app.MapControllers();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Name != "rabbitmq"
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false 
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => true
});

app.MapHealthChecks("/health");

app.Run();
