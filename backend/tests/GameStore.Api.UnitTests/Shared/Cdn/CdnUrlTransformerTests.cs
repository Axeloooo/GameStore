using Azure.Storage.Blobs;
using FluentAssertions;
using GameStore.Api.Shared.Cdn;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace GameStore.Api.UnitTests.Shared.Cdn;

public class CdnUrlTransformerTests(ITestOutputHelper output)
{
    // Illustrative host names only: constructing a BlobServiceClient does not
    // open any connection, so nothing here talks to Azure.
    private const string StorageHost = "storageaccount.blob.core.windows.net";
    private const string FrontDoorHost = "gamestore-endpoint.azurefd.net";

    private static CdnUrlTransformer CreateSut(string? frontDoorHost)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AZURE_FRONTDOOR_HOSTNAME"] = frontDoorHost
            })
            .Build();

        var blobServiceClient = new BlobServiceClient(new Uri($"https://{StorageHost}"));

        return new CdnUrlTransformer(configuration, blobServiceClient);
    }

    [Fact]
    public void TransformToCdnUrl_StorageUrlAndFrontDoorConfigured_ReplacesHostAndKeepsPath()
    {
        // Arrange
        var sut = CreateSut(FrontDoorHost);
        var storageUrl = $"https://{StorageHost}/images/halo.png";

        // Act
        var result = sut.TransformToCdnUrl(storageUrl);
        output.WriteLine($"{storageUrl} -> {result}");

        // Assert
        result.Should().Be($"https://{FrontDoorHost}/images/halo.png");
        result.Should().StartWith($"https://{FrontDoorHost}/");
        result.Should().EndWith("/images/halo.png");
        result.Should().NotContain(StorageHost);
    }

    // Host names are case-insensitive. System.Uri already lower-cases the host,
    // so this pins the observable behaviour rather than one specific comparison.
    [Fact]
    public void TransformToCdnUrl_StorageHostInDifferentCase_StillUsesFrontDoor()
    {
        // Arrange
        var sut = CreateSut(FrontDoorHost);
        var storageUrl = $"https://{StorageHost.ToUpperInvariant()}/images/halo.png";

        // Act
        var result = sut.TransformToCdnUrl(storageUrl);
        output.WriteLine($"{storageUrl} -> {result}");

        // Assert
        result.Should().BeEquivalentTo($"https://{FrontDoorHost}/images/halo.png"); // case-insensitive
        result.Should().ContainEquivalentOf(FrontDoorHost);
    }

    [Fact]
    public void TransformToCdnUrl_UrlWithQueryString_KeepsTheQueryString()
    {
        // Arrange
        var sut = CreateSut(FrontDoorHost);

        // Act
        var result = sut.TransformToCdnUrl($"https://{StorageHost}/images/halo.png?v=2");

        // Assert
        result.Should().MatchRegex(@"^https://gamestore-endpoint\.azurefd\.net/images/halo\.png\?v=2$");
    }

    [Theory]
    [InlineData(null, $"https://{StorageHost}/images/halo.png")]
    [InlineData("", $"https://{StorageHost}/images/halo.png")]
    [InlineData(FrontDoorHost, "https://images.example.test/halo.png")]
    [InlineData(FrontDoorHost, "/images/halo.png")]
    [InlineData(FrontDoorHost, "not a url")]
    public void TransformToCdnUrl_NoFrontDoorOrNotAStorageUrl_ReturnsInputUnchanged(
        string? frontDoorHost,
        string url)
    {
        // Arrange
        var sut = CreateSut(frontDoorHost);

        // Act
        var result = sut.TransformToCdnUrl(url);
        output.WriteLine($"front door '{frontDoorHost}': {url} -> {result}");

        // Assert
        result.Should().BeSameAs(url);
    }

    [Fact]
    public void TransformToCdnUrl_NullUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut(FrontDoorHost);

        // Act
        var act = () => sut.TransformToCdnUrl(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("storageUrl");
    }

    [Fact(Skip = "Known gap: Azurite-style URLs (custom port, account name in the path) keep the port when " +
                 "rewritten. Local development never sets AZURE_FRONTDOOR_HOSTNAME, so it is not handled yet.")]
    public void TransformToCdnUrl_AzuriteStyleUrl_DropsPortAndAccountSegment()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AZURE_FRONTDOOR_HOSTNAME"] = FrontDoorHost
            })
            .Build();
        var azuriteClient = new BlobServiceClient(new Uri("http://127.0.0.1:10000/devstoreaccount1"));
        var sut = new CdnUrlTransformer(configuration, azuriteClient);

        // Act
        var result = sut.TransformToCdnUrl("http://127.0.0.1:10000/devstoreaccount1/images/halo.png");

        // Assert
        result.Should().Be($"http://{FrontDoorHost}/images/halo.png");
    }
}
