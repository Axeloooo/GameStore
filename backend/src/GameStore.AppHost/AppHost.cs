using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Storage;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var entraValidAudience = builder.AddParameter("EntraValidAudience");
var entraAuthority = builder.AddParameter("EntraAuthority");
var allowedOrigins = builder.AddParameter("AllowedOrigins");

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
                .ConfigureInfrastructure(infra =>
                {
                    var resources = infra.GetProvisionableResources();
                    var storageAccount = resources.OfType<StorageAccount>()
                                                  .Single();
                    storageAccount.AllowBlobPublicAccess = true;
                })
                .RunAsEmulator(storage =>
                {
                    storage.WithBlobPort(10000);
                    storage.WithImageTag("3.34.0");
                    storage.WithDataVolume();
                    storage.WithLifetime(ContainerLifetime.Persistent);
                });

var blobs = storage.AddBlobs("Blobs");

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
                .WithExternalHttpEndpoints()
                .PublishAsAzureContainerApp((infra, containerApp) =>
                {
                    var container = containerApp.Template.Containers.Single().Value;

                    container?.Probes.Add(new ContainerAppProbe
                    {
                        ProbeType = ContainerAppProbeType.Liveness,
                        HttpGet = new ContainerAppHttpRequestInfo
                        {
                            Path = "/health/alive",
                            Port = healthPort,
                            Scheme = ContainerAppHttpScheme.Http
                        },
                        PeriodSeconds = 10
                    });

                    container?.Probes.Add(new ContainerAppProbe
                    {
                        ProbeType = ContainerAppProbeType.Readiness,
                        HttpGet = new ContainerAppHttpRequestInfo
                        {
                            Path = "/health/ready",
                            Port = healthPort,
                            Scheme = ContainerAppHttpScheme.Http
                        },
                        PeriodSeconds = 10
                    });

                    containerApp.Template.Scale.MinReplicas = 0;
                    containerApp.Template.Scale.MaxReplicas = 10;
                })
                .WithEnvironment("HTTP_PORTS", $"8080;{healthPort.ToString()}");

if (builder.ExecutionContext.IsPublishMode)
{
    var blobEndpoint = ReferenceExpression.Create(
        $"{storage.GetOutput("blobEndpoint")}"
    );

    var frontDoor = builder.AddBicepTemplate(
        "frontdoor",
        "./bicep/frontdoor.bicep")
        .WithParameter("location", "Global")
        .WithParameter("storageBlobEndpoint", blobEndpoint);

    api.WithEnvironment(
        "AZURE_FRONTDOOR_HOSTNAME",
        frontDoor.GetOutput("frontDoorEndpointHostName"));
    api.WithEnvironment("AllowedOrigins", allowedOrigins);
}

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

    api.WaitFor(keycloak)
        .WithEnvironment(
            "Authentication__Schemes__Keycloak__Authority",
            keycloakAuthority);
}

builder.Build().Run();