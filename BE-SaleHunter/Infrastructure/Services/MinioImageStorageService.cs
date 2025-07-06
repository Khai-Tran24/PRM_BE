using Minio;
using Minio.DataModel.Args;
using System.Text;

namespace BE_SaleHunter.Application.Services
{    public class MinioImageStorageService : IImageStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;
        private readonly ILogger<MinioImageStorageService> _logger;

        public MinioImageStorageService(
            IMinioClient minioClient,
            IConfiguration configuration,
            ILogger<MinioImageStorageService> logger)
        {
            _minioClient = minioClient;
            _bucketName = configuration["MinIO:BucketName"] ?? throw new ArgumentNullException("MinIO:BucketName configuration is required");
            _logger = logger;
        }

        public async Task<string> UploadBase64ImageAsync(string base64Image, string fileName)
        {
            try
            {
                // Remove data URL prefix if present (e.g., "data:image/jpeg;base64,")
                if (base64Image.Contains(","))
                {
                    base64Image = base64Image.Substring(base64Image.IndexOf(",") + 1);
                }

                var imageBytes = Convert.FromBase64String(base64Image);
                
                // Determine file extension from base64 header or default to jpg
                var fileExtension = GetFileExtensionFromBase64(base64Image) ?? "jpg";
                var fullFileName = $"{fileName}.{fileExtension}";

                using var stream = new MemoryStream(imageBytes);
                
                await EnsureBucketExistsAsync();

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fullFileName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType($"image/{fileExtension}");

                await _minioClient.PutObjectAsync(putObjectArgs);

                return await GetImageUrlAsync(fullFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading base64 image: {FileName}", fileName);
                throw;
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file, string fileName)
        {
            try
            {
                var fileExtension = Path.GetExtension(file.FileName).TrimStart('.');
                var fullFileName = $"{fileName}.{fileExtension}";

                using var stream = file.OpenReadStream();
                
                await EnsureBucketExistsAsync();

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fullFileName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType(file.ContentType);

                await _minioClient.PutObjectAsync(putObjectArgs);

                return await GetImageUrlAsync(fullFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image file: {FileName}", fileName);
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string fileName)
        {
            try
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName);

                await _minioClient.RemoveObjectAsync(removeObjectArgs);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {FileName}", fileName);
                return false;
            }
        }

        public async Task<string> GetImageUrlAsync(string fileName)
        {
            try
            {
                var presignedGetObjectArgs = new PresignedGetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithExpiry(60 * 60 * 24); // 24 hours

                return await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image URL: {FileName}", fileName);
                throw;
            }
        }

        public async Task<byte[]> GetImageAsync(string fileName)
        {
            try
            {
                using var stream = new MemoryStream();
                
                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithCallbackStream(async (streamData) =>
                    {
                        await streamData.CopyToAsync(stream);
                    });

                await _minioClient.GetObjectAsync(getObjectArgs);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image bytes: {FileName}", fileName);
                throw;
            }
        }

        private async Task EnsureBucketExistsAsync()
        {
            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(_bucketName);

                bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs);
                if (!found)
                {
                    var makeBucketArgs = new MakeBucketArgs()
                        .WithBucket(_bucketName);

                    await _minioClient.MakeBucketAsync(makeBucketArgs);
                    _logger.LogInformation("Created MinIO bucket: {BucketName}", _bucketName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring bucket exists: {BucketName}", _bucketName);
                throw;
            }
        }

        private string? GetFileExtensionFromBase64(string base64)
        {
            try
            {
                var header = base64.Substring(0, Math.Min(50, base64.Length));
                if (header.Contains("jpeg") || header.Contains("jpg"))
                    return "jpg";
                if (header.Contains("png"))
                    return "png";
                if (header.Contains("gif"))
                    return "gif";
                if (header.Contains("webp"))
                    return "webp";
                
                return "jpg"; // Default
            }
            catch
            {
                return "jpg"; // Default
            }
        }
    }
}
