using Azure.Storage.Blobs;

namespace GameStore.Api.Shared.Cdn;

public class CdnUrlTransformer(
    IConfiguration configuration,
    BlobServiceClient blobServiceClient)
{
    public string TransformToCdnUrl(string storageUrl)
    {
        ArgumentNullException.ThrowIfNull(storageUrl);

        var frontDoorHost = configuration["AZURE_FRONTDOOR_HOSTNAME"];

        if (string.IsNullOrEmpty(frontDoorHost))
        {
            return storageUrl;
        }

        if (Uri.TryCreate(storageUrl, UriKind.Absolute, out var uri))
        {
            var storageHost = blobServiceClient.Uri.Host;

            if (!string.Equals(uri.Host, storageHost, StringComparison.OrdinalIgnoreCase))
            {
                return storageUrl;
            }

            var uriBuilder = new UriBuilder(uri)
            {
                Host = frontDoorHost
            };

            return uriBuilder.Uri.ToString();
        }

        return storageUrl;
    }
}
