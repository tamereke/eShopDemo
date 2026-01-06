using MassTransit;
using NotificationService.Worker.Consumers;
using Shared.Contracts;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add HttpClient for Gateway to send monitor logs
builder.Services.AddHttpClient("Gateway", client => client.BaseAddress = new Uri("http://gateway"));

builder.Services.AddMassTransit(x =>
{
    x.AddRider(rider =>
    {
        rider.AddConsumer<IntegrationEventConsumer>();
        rider.UsingKafka((context, k) =>
        {
            k.Host(builder.Configuration.GetConnectionString("kafka"));
            
            k.TopicEndpoint<IntegrationEvent>("order-events", "notification-group", e =>
            {
                e.ConfigureConsumer<IntegrationEventConsumer>(context);
                e.CreateIfMissing(t => 
                {
                    t.NumPartitions = 1;
                    t.ReplicationFactor = 1;
                });
            });

            k.TopicEndpoint<Shared.Contracts.Events.OrderConfirmedEvent>("order-confirmed", "notification-group", e =>
            {
                e.ConfigureConsumer<IntegrationEventConsumer>(context);
                e.CreateIfMissing(t => 
                {
                    t.NumPartitions = 1;
                    t.ReplicationFactor = 1;
                });
            });

            k.TopicEndpoint<Shared.Contracts.Events.OrderCancelledEvent>("order-cancelled", "notification-group", e =>
            {
                e.ConfigureConsumer<IntegrationEventConsumer>(context);
                e.CreateIfMissing(t => 
                {
                    t.NumPartitions = 1;
                    t.ReplicationFactor = 1;
                });
            });
            
            k.TopicEndpoint<IntegrationEvent>("inventory-events", "notification-group", e =>
            {
                e.ConfigureConsumer<IntegrationEventConsumer>(context);
                e.CreateIfMissing(t => 
                {
                    t.NumPartitions = 1;
                    t.ReplicationFactor = 1;
                });
            });
            
            k.TopicEndpoint<IntegrationEvent>("payment-events", "notification-group", e =>
            {
                e.ConfigureConsumer<IntegrationEventConsumer>(context);
                e.CreateIfMissing(t => 
                {
                    t.NumPartitions = 1;
                    t.ReplicationFactor = 1;
                });
            });
        });
    });

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
