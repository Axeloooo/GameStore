using Azure.Identity;
using GameStore.Contracts.Orders;
using GameStore.Data;
using GameStore.Worker.MessageHandlers;
using GameStore.Worker.MessageHandlers.Orders;

namespace GameStore.Worker;

public static class WorkerHostFactory
{
    public static IHost Build(
        string[]? args = null,
        string? environmentName = null,
        Action<IConfigurationBuilder>? configure = null,
        Action<IServiceCollection>? testOverrides = null)
    {
        var settings = new HostApplicationBuilderSettings { Args = args };
        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            settings.EnvironmentName = environmentName;
        }

        var builder = Host.CreateApplicationBuilder(settings);

        configure?.Invoke(builder.Configuration); // Allow test overrides

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

        testOverrides?.Invoke(builder.Services); // Allow test overrides

        return builder.Build();
    }
}
