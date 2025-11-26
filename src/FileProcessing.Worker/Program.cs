using FileProcessing.Application.Interfaces;
using FileProcessing.Infrastructure.Persistence;
using FileProcessing.Infrastructure.Repositories;
using FileProcessing.Infrastructure.Storage;
using FileProcessing.Worker.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;

try
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build())
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "FileProcessingWorker")
        .CreateLogger();

    Log.Information("Starting Worker...");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            // Database
            services.AddDbContext<FileProcessingDbContext>(options =>
                options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

            // Repositórios e serviços
            services.AddScoped<IFileRecordRepository, FileRecordRepository>();
            services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();

            // MassTransit
            var rabbit = context.Configuration.GetSection("RabbitMQ");
            services.AddMassTransit(x =>
            {
                x.AddConsumer<FileUploadedConsumer>();
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(rabbit["Host"], "/", h =>
                    {
                        h.Username(rabbit["Username"]);
                        h.Password(rabbit["Password"]);
                    });

                    cfg.ReceiveEndpoint("upload_queue", e =>
                    {
                        e.ConfigureConsumer<FileUploadedConsumer>(ctx);
                        e.PrefetchCount = 16;
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                        e.DiscardFaultedMessages();
                    });
                });
            });

            services.AddMassTransitHostedService();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker crashed");
}
finally
{
    Log.CloseAndFlush();
}
