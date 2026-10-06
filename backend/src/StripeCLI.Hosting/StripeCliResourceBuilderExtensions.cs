using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace StripeCLI.Hosting;

public static class StripeCliResourceBuilderExtensions
{
    private const string Image = "stripe/stripe-cli";
    private const string Tag = "v1.32.0";
    private const string ApiKeyForCliEnvVarName = "STRIPE_API_KEY";
    private const string ApiKeyForReferenceEnvVarName = "Stripe__SecretKey";
    private const string SecretsContainerPath = "/secrets";
    private const string EndpointSecretEnvVarName = "Stripe__EndpointSecret";

    public static IResourceBuilder<StripeCliResource> AddStripeCli(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource> stripeApiKey
    )
    {
        var resource = new StripeCliResource(name, stripeApiKey.Resource);

        return builder.AddResource(resource)
                        .WithImage(Image)
                        .WithImageTag(Tag)
                        .WithEnvironment(ApiKeyForCliEnvVarName, resource.ApiKey);
    }

    public static IResourceBuilder<StripeCliResource> WithWebhookEventListener(
        this IResourceBuilder<StripeCliResource> builder,
        ReferenceExpression forwardToEndpoint,
        string webhookSecretFilePath
    )
    {
        builder.WithArgs("listen", "--forward-to", forwardToEndpoint);
        builder.Resource.EndpointSecretFilePath = webhookSecretFilePath;
        return builder;
    }

    public static IResourceBuilder<StripeCliResource> WithPrintSecret(
        this IResourceBuilder<StripeCliResource> builder,
        string webhookSecretFilePath)
    {
        var secretsDirectory = Path.GetDirectoryName(webhookSecretFilePath)
                        ?? throw new ArgumentException(
                            "Invalid secrets file path",
                            nameof(webhookSecretFilePath)
                        );
        var webhookSecretFileName = Path.GetFileName(webhookSecretFilePath);

        return builder.WithBindMount(secretsDirectory, SecretsContainerPath)
                        .WithEntrypoint("/bin/sh")
                        .WithArgs("-c", $"stripe listen --print-secret > {SecretsContainerPath}/{webhookSecretFileName}");
    }

    public static IResourceBuilder<TDestination> WithReference<TDestination>(
        this IResourceBuilder<TDestination> builder,
        IResourceBuilder<StripeCliResource> source
    ) where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[ApiKeyForReferenceEnvVarName] = source.Resource.ApiKey;

            if (!string.IsNullOrEmpty(source.Resource.EndpointSecretFilePath))
            {
                var endpointSecret = File.ReadAllText(source.Resource.EndpointSecretFilePath)
                                         .Trim();
                context.EnvironmentVariables[EndpointSecretEnvVarName] = endpointSecret;
            }
        });
    }
}
