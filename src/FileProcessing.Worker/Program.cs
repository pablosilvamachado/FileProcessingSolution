using FileProcessing.Application.Interfaces;
using FileProcessing.Infrastructure.Persistence;
using FileProcessing.Infrastructure.Repositories;
using FileProcessing.Infrastructure.Storage;
using FileProcessing.Worker.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://seq:5341")
    .CreateLogger();

builder.Services.AddDbContext<FileProcessingDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<IFileRecordRepository, FileRecordRepository>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<FileUploadedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("upload_queue", e =>
        {
            e.ConfigureConsumeTopology = false;

            e.Bind("upload_exchange", s =>
            {
                s.RoutingKey = "file_uploaded";
                s.ExchangeType = "direct";
            });

            e.SetQueueArgument("x-dead-letter-exchange", "upload_exchange_dlq");
            e.SetQueueArgument("x-dead-letter-routing-key", "file_uploaded_dlq");

            e.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));

            e.ConfigureConsumer<FileUploadedConsumer>(context);
        });

        cfg.ReceiveEndpoint("upload_queue_retry", e =>
        {
            e.ConfigureConsumeTopology = false;

            e.Bind("upload_exchange_retry", s => {
                s.RoutingKey = "file_uploaded_retry";
                s.ExchangeType = "direct";
            });

            e.SetQueueArgument("x-message-ttl", 30000);
            e.SetQueueArgument("x-dead-letter-exchange", "upload_exchange");
            e.SetQueueArgument("x-dead-letter-routing-key", "file_uploaded");
        });

        cfg.ReceiveEndpoint("upload_queue_dlq", e =>
        {
            e.ConfigureConsumeTopology = false;

            e.Bind("upload_exchange_dlq", s => {
                s.RoutingKey = "file_uploaded_dlq";
                s.ExchangeType = "direct";
            });
        });
    });
});


var app = builder.Build();
await app.RunAsync();
