namespace BE_SaleHunter.Application.Services
{
    public interface IImageStorageService
    {
        Task<string> UploadBase64ImageAsync(string base64Image, string fileName);
        Task<string> UploadImageAsync(IFormFile file, string fileName);
        Task<bool> DeleteImageAsync(string fileName);
        Task<string> GetImageUrlAsync(string fileName);
        Task<byte[]> GetImageAsync(string fileName);
    }
}
