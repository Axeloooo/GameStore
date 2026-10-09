using Aspire.Hosting.ApplicationModel;

namespace StripeCLI.Hosting;

public class StripeCliResource(string name, ParameterResource apiKey)
    : ContainerResource(name)
{
    public ParameterResource ApiKey { get; } = apiKey;
    public string? EndpointSecretFilePath { get; internal set; }
}
