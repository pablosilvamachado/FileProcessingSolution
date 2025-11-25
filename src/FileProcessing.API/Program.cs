using FileProcessing.Api.Services;
using FileProcessing.API.Configurations;
using FileProcessing.API.Services;
using FileProcessing.Application.Interfaces;
using FileProcessing.Infrastructure.Persistence;
using FileProcessing.Infrastructure.Repositories;
using FileProcessing.Infrastructure.Storage;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// Serilog
// ---------------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ---------------------------
// Database (Postgres via Docker)
// ---------------------------
builder.Services.AddDbContext<FileProcessingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.MigrationsAssembly("FileProcessing.Infrastructure")));

builder.Services.AddScoped<IFileRecordRepository, FileRecordRepository>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IMessageProducerService, MessageProducerService>();

// ---------------------------
// MassTransit (RabbitMQ Producer)
// ---------------------------
var rabbit = builder.Configuration.GetSection("RabbitMQ");

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbit["Host"], "/", h =>
        {
            h.Username(rabbit["Username"]);
            h.Password(rabbit["Password"]);
        });
    });
});

builder.Services.AddMassTransitHostedService();

// ---------------------------
// JWT Authentication
// ---------------------------
builder.Services.Configure<TokenOptions>(builder.Configuration.GetSection("Jwt"));
var tokenOptions = builder.Configuration.GetSection("Jwt").Get<TokenOptions>();

builder.Services.AddSingleton<ITokenService, TokenService>();

var key = Encoding.UTF8.GetBytes(tokenOptions.Key);

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

// ---------------------------
// Authorization
// ---------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustBeUser", policy => policy.RequireRole("User"));
});

// ---------------------------
// HealthChecks
// ---------------------------
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres")
    .AddRabbitMQ(
        $"amqp://{rabbit["Username"]}:{rabbit["Password"]}@{rabbit["Host"]}:{rabbit["Port"]}/",
        name: "rabbitmq");

// ---------------------------
// CORS
// ---------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ---------------------------
// Controllers + Swagger
// ---------------------------
builder.Services.AddControllers();
builder.Services.AddSwaggerWithJwt();

// ---------------------------
// Build
// ---------------------------
var app = builder.Build();

try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<FileProcessingDbContext>();
        db.Database.Migrate();
    }
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
