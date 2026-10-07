using Azure.Identity;
using GameStore.Contracts.Orders;
using GameStore.Data;
using GameStore.Worker;
using GameStore.Worker.MessageHandlers;
using GameStore.Worker.MessageHandlers.Orders;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = builder.Configuration["AZURE_CLIENT_ID"]
});

builder.AddGameStoreNpgsql<GameStoreContext>("GameStoreDB", credential);

builder.AddAzureServiceBusClient(
    "serviceBus",
    settings => settings.Credential = credential
);

builder.Services.AddScoped<IMessageHandler<OrderPaid>, OrderPaidHandler>();
builder.Services.AddHostedService<OrdersQueueProcessor>();
builder.Services.AddHostedService<OrdersDeadLetterQueueProcessor>();

var host = builder.Build();
host.Run();
