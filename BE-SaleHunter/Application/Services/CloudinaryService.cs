using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace BE_SaleHunter.Application.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "your_folder_name"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            return uploadResult.SecureUrl.AbsoluteUri;
        }
    }
}
