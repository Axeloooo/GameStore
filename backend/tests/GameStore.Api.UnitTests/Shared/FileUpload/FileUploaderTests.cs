using Azure.Storage.Blobs;
using FluentAssertions;
using GameStore.Api.Shared.FileUpload;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GameStore.Api.UnitTests.Shared.FileUpload;

// Validation path only. FileUploader has no internal helper: size and extension
// checks live inline in the public UploadFileAsync, so that is the seam under
// test. The blob client is a substitute that throws as soon as it is touched,
// which tells us a file got past validation without any real storage account.
public class FileUploaderTests
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MiB
    private const string Folder = "images";

    private readonly BlobServiceClient blobServiceClientMock = Substitute.For<BlobServiceClient>();
    private readonly FileUploader sut;

    public FileUploaderTests()
    {
        blobServiceClientMock
            .GetBlobContainerClient(Arg.Any<string>())
            .Throws(new StorageReachedException());

        sut = new FileUploader(blobServiceClientMock);
    }

    // The content is never read during validation, so even "50 MiB" files
    // cost nothing: FormFile only reports the length it was given.
    private static IFormFile CreateFile(long length, string fileName = "cover.png")
        => new FormFile(Stream.Null, 0, length, "ImageFile", fileName);

    private async Task<bool> PassesValidationAsync(IFormFile file)
    {
        try
        {
            await sut.UploadFileAsync(file, Folder);
            return false;
        }
        catch (StorageReachedException)
        {
            return true;
        }
    }

    [Fact]
    public async Task UploadFileAsync_NullFile_ReturnsNoFileUploadedError()
    {
        // Act
        var result = await sut.UploadFileAsync(null!, Folder);

        // Assert
        result.IsSucess.Should().BeFalse();
        result.ErrorMessage.Should().Be("No file uploaded");
        result.FileUrl.Should().BeNull();
    }

    [Fact]
    public async Task UploadFileAsync_EmptyFile_ReturnsNoFileUploadedError()
    {
        // Act
        var result = await sut.UploadFileAsync(CreateFile(0), Folder);

        // Assert
        result.IsSucess.Should().BeFalse();
        result.ErrorMessage.Should().Be("No file uploaded");
    }

    [Theory]
    [InlineData(MaxFileSize + 1)]
    [InlineData(2 * MaxFileSize)]
    [InlineData(50L * 1024 * 1024)]
    public async Task UploadFileAsync_FileLargerThanLimit_ReturnsTooLargeWithoutTouchingStorage(long length)
    {
        // Act
        var result = await sut.UploadFileAsync(CreateFile(length), Folder);

        // Assert
        result.IsSucess.Should().BeFalse();
        result.ErrorMessage.Should().Be("File is too large.");
        blobServiceClientMock.DidNotReceive().GetBlobContainerClient(Arg.Any<string>());
    }

    [Fact]
    public async Task UploadFileAsync_SizesAroundTheLimit_AcceptsUpToExactlyTenMebibytes()
    {
        // Arrange
        long[] sizes = [1, MaxFileSize - 1, MaxFileSize, MaxFileSize + 1, 2 * MaxFileSize];

        // Act
        var outcomes = new List<(long Size, bool Accepted)>();
        foreach (var size in sizes)
        {
            outcomes.Add((size, await PassesValidationAsync(CreateFile(size))));
        }

        var largestAccepted = outcomes.Where(o => o.Accepted).Max(o => o.Size);
        var smallestRejected = outcomes.Where(o => !o.Accepted).Min(o => o.Size);

        // Assert
        largestAccepted.Should().Be(10_485_760);
        smallestRejected.Should().Be(10_485_761);
        (smallestRejected - largestAccepted).Should().Be(1, "the limit is inclusive");
        outcomes.Where(o => o.Accepted).Select(o => o.Size)
                .Should().AllSatisfy(size => size.Should().BeInRange(1, MaxFileSize));
        outcomes.Count(o => o.Accepted).Should().Be(3);
    }

    [Theory]
    [InlineData("cover.gif")]
    [InlineData("cover.exe")]
    [InlineData("cover.png.exe")]
    [InlineData("cover")]
    [InlineData("cover.")]
    public async Task UploadFileAsync_NotAnAllowedImageExtension_ReturnsInvalidFileType(string fileName)
    {
        // Act
        var result = await sut.UploadFileAsync(CreateFile(1024, fileName), Folder);

        // Assert
        result.IsSucess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid file type.");
        blobServiceClientMock.DidNotReceive().GetBlobContainerClient(Arg.Any<string>());
    }

    [Theory]
    [InlineData("cover.png")]
    [InlineData("cover.jpg")]
    [InlineData("cover.jpeg")]
    [InlineData("COVER.PNG")]
    [InlineData("my.cover.JpEg")]
    public async Task UploadFileAsync_ValidImage_PassesValidationAndReachesStorage(string fileName)
    {
        // Act
        var act = () => sut.UploadFileAsync(CreateFile(1024, fileName), Folder);

        // Assert
        await act.Should().ThrowExactlyAsync<StorageReachedException>();
        blobServiceClientMock.Received(1).GetBlobContainerClient(Folder);
    }

    [Fact]
    public async Task UploadFileAsync_StorageFailsAfterValidation_PropagatesTheException()
    {
        // Arrange
        blobServiceClientMock
            .GetBlobContainerClient(Arg.Any<string>())
            .Throws(new InvalidOperationException("Storage account unreachable"));

        // Act
        var act = () => sut.UploadFileAsync(CreateFile(1024), Folder);

        // Assert: storage failures are not turned into a FileUploadResult.
        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*unreachable*");
    }

    private sealed class StorageReachedException() : Exception("Validation passed and storage was reached.");
}
