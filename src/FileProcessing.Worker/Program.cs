using FileProcessing.Application.Interfaces;
using FileProcessing.Infrastructure.Persistence;
using FileProcessing.Infrastructure.Repositories;
using FileProcessing.Infrastructure.Storage;
using FileProcessing.Worker.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Logging
builder.Services.AddSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(builder.Configuration));

// Database
builder.Services.AddDbContext<FileProcessingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IFileRecordRepository, FileRecordRepository>();
builder.Services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<FileUploadedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var rabbit = builder.Configuration.GetSection("RabbitMQ");

        cfg.Host(rabbit["Host"], "/", h =>
        {
            h.Username(rabbit["Username"]);
            h.Password(rabbit["Password"]);
        });

        cfg.ReceiveEndpoint("upload_queue", e =>
        {
            e.ConfigureConsumer<FileUploadedConsumer>(ctx);

            e.PrefetchCount = 16;

            // Retry lógico (não usa fila)
            e.UseMessageRetry(r =>
            {
                r.Immediate(3);
            });

            // Redelivery usando FILA "_retry"
            e.UseDelayedRedelivery(r =>
            {
                r.Intervals(
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(60)
                );
            });

            // Importante!
            e.UseInMemoryOutbox();
        });
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"), name: "postgres")
    .AddRabbitMQ(
        sp =>
        {
            var uri = builder.Configuration["RabbitMQ:Connection"];
            var factory = new ConnectionFactory { Uri = new Uri(uri) };
            return factory.CreateConnectionAsync(CancellationToken.None);
        },
        name: "rabbitmq");

// Build and run Worker
var host = builder.Build();
await host.RunAsync();
