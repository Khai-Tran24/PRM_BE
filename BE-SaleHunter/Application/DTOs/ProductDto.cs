using System.ComponentModel.DataAnnotations;

namespace BE_SaleHunter.Application.DTOs
{
    public class CreateProductDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int? SalePercent { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        public List<string> Images { get; set; } = new(); // Base64 images
    }

    public class UpdateProductDto
    {
        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Price { get; set; }

        [Range(0, double.MaxValue)]
        public int? SalePercent { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public List<string> NewImages { get; set; } = new(); // Base64 images
    }

    public class ProductDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal CurrentPrice { get; set; }
        public int? SalePercent { get; set; }
        public decimal FinalPrice => SalePercent.Value > 0 ? CurrentPrice * (1 - SalePercent.Value / 100m) : CurrentPrice;
        public string? Brand { get; set; }
        public string Category { get; set; } = string.Empty;
        public List<ProductImageDto> Images { get; set; } = new();
        public string? MainImage => Images.FirstOrDefault(i => i.IsMainImage)?.ImageUrl ?? Images.FirstOrDefault()?.ImageUrl;
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public bool IsFavorite { get; set; }
        public long StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string? StoreImageUrl { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductImageDto
    {
        public long Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMainImage { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ProductPriceDto
    {
        public long Id { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public bool IsCurrentPrice { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ProductRatingDto
    {
        public long Id { get; set; }
        public int Rating { get; set; }
        public string? Review { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class CreateProductRatingDto
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }

    public class ProductSearchResultDto
    {
        public List<ProductDto> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
