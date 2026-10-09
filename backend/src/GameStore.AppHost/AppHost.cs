using Projects;
using StripeCLI.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var entraValidAudience = builder.AddParameter("EntraValidAudience");
var entraAuthority = builder.AddParameter("EntraAuthority");
var stripeApiKey = builder.AddParameter("StripeApiKey", secret: true);
var checkoutReturnUrl = builder.AddParameter("CheckoutReturnUrl");

var database = builder.AddAzurePostgresFlexibleServer("postgres")
                    .RunAsContainer(postgres =>
                    {
                        postgres.WithHostPort(5432);
                        postgres.WithImageTag("17.4");
                        postgres.WithDataVolume();
                        postgres.WithLifetime(ContainerLifetime.Persistent);
                        postgres.WithPgAdmin(pgAdmin =>
                        {
                            pgAdmin.WithHostPort(5050);
                            pgAdmin.WithLifetime(ContainerLifetime.Persistent);
                        });
                    })
                    .AddDatabase("GameStoreDB", "gamestore");

var storage = builder.AddAzureStorage("storage")
                .RunAsEmulator(storage =>
                {
                    storage.WithBlobPort(10000);
                    storage.WithImageTag("3.34.0");
                    storage.WithDataVolume();
                    storage.WithLifetime(ContainerLifetime.Persistent);
                });

var blobs = storage.AddBlobs("Blobs");

var serviceBus = builder.AddAzureServiceBus("serviceBus")
                        .RunAsEmulator(emulator =>
                        {
                            emulator.WithLifetime(ContainerLifetime.Persistent);
                        });

serviceBus.AddServiceBusQueue("orders")
            .WithProperties(queue => queue.RequiresDuplicateDetection = true);

var healthPort = 8081;

var api = builder.AddProject<GameStore_Api>("gamestore-api")
                .WithReference(database)
                .WaitFor(database)
                .WithReference(blobs)
                .WaitFor(blobs)
                .WithEnvironment(
                    "Authentication__Schemes__Entra__ValidAudience",
                    entraValidAudience)
                .WithEnvironment(
                    "Authentication__Schemes__Entra__Authority",
                    entraAuthority)
                .WithEnvironment("Stripe__CheckoutReturnUrl",
                    checkoutReturnUrl)
                .WithReference(serviceBus)
                .WaitFor(serviceBus)
                .WithExternalHttpEndpoints()
                .WithEnvironment("HTTP_PORTS", $"8080;{healthPort.ToString()}");

var worker = builder.AddProject<GameStore_Worker>("gamestore-worker")
                    .WithReference(database)
                    .WaitFor(database)
                    .WithReference(serviceBus)
                    .WaitFor(serviceBus);

if (builder.ExecutionContext.IsRunMode)
{
    var keycloak = builder.AddKeycloak("keycloak", port: 8080)
                        .WithImageTag("26.0.7")
                        .WithDataVolume()
                        .WithLifetime(ContainerLifetime.Persistent)
                        .WithRealmImport("../../localinfra");

    var keycloakAuthority = ReferenceExpression.Create(
        $"{keycloak.GetEndpoint("http").Property(EndpointProperty.Url)}/realms/gamestore"
    );

    var webhookSecretFilePath = Path.GetFullPath(Path.Combine(
        builder.AppHostDirectory,
        "..", "..", ".stripe",
        "webhook_secret.txt"
    ));

    var stripeSecretGenerator = builder.AddStripeCli("stripeSecretGen", stripeApiKey)
                                        .WithPrintSecret(webhookSecretFilePath);

    var forwardExpression = ReferenceExpression.Create(
        $"{api.GetEndpoint("http")}/payments/stripe-webhook"
    );

    var stripeListener = builder.AddStripeCli("stripeListener", stripeApiKey)
                                .WithWebhookEventListener(
                                    forwardExpression,
                                    webhookSecretFilePath);

    api.WaitFor(keycloak)
        .WithEnvironment(
            "Authentication__Schemes__Keycloak__Authority",
            keycloakAuthority)
        .WithReference(stripeListener)
        .WaitFor(stripeListener)
        .WaitForCompletion(stripeSecretGenerator);
}

builder.Build().Run();