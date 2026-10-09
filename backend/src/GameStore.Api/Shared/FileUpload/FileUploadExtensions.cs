using Azure.Core;
using Azure.Storage.Blobs;

namespace GameStore.Api.Shared.FileUpload;

public static class FileUploadExtensions
{
    public static void AddFileUploader(
        this WebApplicationBuilder builder,
        TokenCredential credential)
    {
        builder.AddAzureBlobServiceClient("Blobs", settings =>
        {
            if (builder.Environment.IsProduction())
            {
                settings.Credential = credential;
            }
        });

        builder.Services.AddSingleton<FileUploader>();
    }
}
