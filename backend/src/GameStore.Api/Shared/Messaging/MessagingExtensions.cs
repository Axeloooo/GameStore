using Azure.Core;

namespace GameStore.Api.Shared.Messaging;

public static class MessagingExtensions
{
    public static WebApplicationBuilder AddMessaging(
        this WebApplicationBuilder builder,
        string connectionName,
        TokenCredential credential
    )
    {
        builder.AddAzureServiceBusClient(
            connectionName,
            settings => settings.Credential = credential
        );

        builder.Services.AddSingleton<IMessagePublisher, ServiceBusMessagePublisher>();

        return builder;
    }
}
