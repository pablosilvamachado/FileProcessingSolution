using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using FileProcessing.Infrastructure.Persistence;
using FileProcessing.Infrastructure.Repositories;
using FileProcessing.Infrastructure.Storage;
using FileProcessing.Worker.Consumers;
using FileProcessing.Application.Interfaces;

var builder = Host.CreateApplicationBuilder(args);

// Serilog (configuração explícita - sem ReadFrom.Configuration)
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://seq:5341")
    .CreateLogger();

// DbContext
builder.Services.AddDbContext<FileProcessingDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// DI
builder.Services.AddScoped<IFileRecordRepository, FileRecordRepository>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// MassTransit with advanced topology (RabbitMQ) - DLQ + Retry queue
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<FileUploadedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        // upload_queue (main)
        cfg.ReceiveEndpoint("upload_queue", e =>
        {
            e.ConfigureConsumeTopology = false;

            e.Bind("upload_queue", s => {
                s.RoutingKey = "upload_queue";
                s.ExchangeType = "direct";
            });

            // set DLX to send failing messages to upload_queue_dlq
            e.SetQueueArgument("x-dead-letter-exchange", "upload_queue_dlq");
            e.SetQueueArgument("x-dead-letter-routing-key", "upload_queue_dlq");

            // MassTransit retry (message-level)
            e.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));

            e.ConfigureConsumer<FileUploadedConsumer>(context);
        });

        // retry queue: TTL -> dead-letter back to main queue
        cfg.ReceiveEndpoint("upload_queue_retry", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind("upload_queue_retry", s => {
                s.RoutingKey = "upload_queue_retry";
                s.ExchangeType = "direct";
            });

            e.SetQueueArgument("x-message-ttl", 30000); // 30s
            e.SetQueueArgument("x-dead-letter-exchange", "upload_queue");
            e.SetQueueArgument("x-dead-letter-routing-key", "upload_queue");
        });

        // DLQ endpoint
        cfg.ReceiveEndpoint("upload_queue_dlq", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind("upload_queue_dlq", s => {
                s.RoutingKey = "upload_queue_dlq";
                s.ExchangeType = "direct";
            });

            // You can optionally configure a consumer here to persist DLQ messages
        });
    });
});

var app = builder.Build();
await app.RunAsync();
