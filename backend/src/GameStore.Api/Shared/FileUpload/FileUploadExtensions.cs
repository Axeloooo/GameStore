using Azure.Core;
using Azure.Storage.Blobs;

namespace GameStore.Api.Shared.FileUpload;

public static class FileUploadExtensions
{
    public static void AddFileUploader(
        this WebApplicationBuilder builder, 
        TokenCredential credential)
    {
        builder.Services.AddSingleton(serviceProvider =>
                        {
                            var config = serviceProvider.GetRequiredService<IConfiguration>();
                            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

                            var connectionString = config.GetConnectionString("Blobs")
                                ?? throw new InvalidOperationException("Storage url is missing");

                            return environment.IsDevelopment() ?
                                new BlobServiceClient(connectionString) :
                                new BlobServiceClient(
                                    new Uri(connectionString),
                                    credential);
                        })
                        .AddSingleton<FileUploader>();
    }
}
