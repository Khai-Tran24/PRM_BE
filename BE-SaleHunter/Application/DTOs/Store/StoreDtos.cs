using System.ComponentModel.DataAnnotations;
using BE_SaleHunter.Application.DTOs;

namespace BE_SaleHunter.Application.DTOs.Store
{
    public class CreateStoreDto
    {
        public string? LogoBase64 { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty; // niche_market

        [StringLength(500)]
        public string? Address { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? WhatsappPhoneNumber { get; set; }

        [StringLength(200)]
        public string? WebsiteLink { get; set; }

        [StringLength(200)]
        public string? FacebookLink { get; set; }

        [StringLength(200)]
        public string? InstagramLink { get; set; }
    }    public class UpdateStoreDto
    {
        public string? LogoBase64 { get; set; }

        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? WhatsappPhoneNumber { get; set; }

        [StringLength(200)]
        public string? WebsiteLink { get; set; }

        [StringLength(200)]
        public string? FacebookLink { get; set; }

        [StringLength(200)]
        public string? InstagramLink { get; set; }
    }

    public class StoreDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string Category { get; set; } = string.Empty; // StoreCategory
        public string? Description { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? WhatsappPhone { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public long UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StorePageDto : BaseResponseDto
    {
        public StoreDto? Store { get; set; }
        public int ProductsLength { get; set; }
        public List<ProductDto> Products { get; set; } = new();
    }

    public class CreateStoreResponseDto : BaseResponseDto
    {
        public StoreDto? Store { get; set; }
    }
}
