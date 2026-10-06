using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace GameStore.Api.Shared.FileUpload;

public class FileUploader(BlobServiceClient blobServiceClient)
{
    public async Task<FileUploadResult> UploadFileAsync(
        IFormFile file,
        string folder)
    {
        var result = new FileUploadResult();

        if (file == null || file.Length == 0)
        {
            result.IsSucess = false;
            result.ErrorMessage = "No file uploaded";
            return result;
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            result.IsSucess = false;
            result.ErrorMessage = "File is too large.";
            return result;
        }

        var permittedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (
            string.IsNullOrEmpty(fileExtension)
            || !permittedExtensions.Contains(fileExtension))
        {
            result.IsSucess = false;
            result.ErrorMessage = "Invalid file type.";
            return result;
        }

        var containerClient = blobServiceClient.GetBlobContainerClient(folder);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var safeFileName = $"{Guid.NewGuid()}{fileExtension}";
        
        var blobClient = containerClient.GetBlobClient(safeFileName);
        await blobClient.DeleteIfExistsAsync();

        using var fileStream = file.OpenReadStream();
        await blobClient.UploadAsync(
            fileStream,
            new BlobHttpHeaders { ContentType = file.ContentType }
        );

        result.IsSucess = true;
        result.FileUrl = blobClient.Uri.ToString();
        return result;
    }
}
