using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);


var sqlServer = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Session);
var orderDb = sqlServer.AddDatabase("OrderDb");
var catalogDb = sqlServer.AddDatabase("CatalogDb");

var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Session);

var seq = builder.AddSeq("seq")
    .WithLifetime(ContainerLifetime.Session);

var rabbitMq = builder.AddRabbitMQ("rabbitmq-msg")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Session);

var kafka = builder.AddKafka("kafka")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithLifetime(ContainerLifetime.Session);


var catalogService = builder.AddProject<Projects.CatalogService_Api>("catalogservice")
    .WithReference(catalogDb)
    .WithReference(seq);

var basketService = builder.AddProject<Projects.BasketService_Api>("basketservice")
    .WithReference(redis)
    .WithReference(seq);

var inventoryService = builder.AddProject<Projects.InventoryService_Api>("inventoryservice")
    .WithReference(rabbitMq)
    .WithReference(kafka)
    .WithReference(seq);

var orderService = builder.AddProject<Projects.OrderService_Api>("orderservice")
    .WithReference(orderDb)
    .WithReference(redis)
    .WithReference(rabbitMq)
    .WithReference(kafka)
    .WithReference(inventoryService)
    .WithReference(seq);

var paymentService = builder.AddProject<Projects.PaymentService_Api>("paymentservice")
    .WithReference(rabbitMq)
    .WithReference(kafka)
    .WithReference(seq);

var notificationWorker = builder.AddProject<Projects.NotificationService_Worker>("notificationworker")
    .WithReference(kafka)
    .WithReference(seq)
    .WaitFor(kafka);

var gateway = builder.AddProject<Projects.Gateway_Api>("gateway")
    .WithReference(catalogService)
    .WithReference(orderService)
    .WithReference(inventoryService)
    .WithReference(paymentService)
    .WithReference(basketService)
    .WithReference(seq)
    .WithExternalHttpEndpoints();

orderService.WithReference(gateway);
notificationWorker.WithReference(gateway);
inventoryService.WithReference(gateway);
paymentService.WithReference(gateway);

builder.AddJavaScriptApp("webui", "../Web.UI", "dev")
    .WithReference(gateway)
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
