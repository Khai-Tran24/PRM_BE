using AutoMapper;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Store;

namespace BE_SaleHunter.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.HasStore, opt => opt.MapFrom(src => src.HasStore()))
                .ForMember(dest => dest.AccountType, opt => opt.MapFrom(src => src.GetAccountType()));            // Store mappings
            CreateMap<Store, StoreDto>();            // Product mappings
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
                .ForMember(dest => dest.StoreName, opt => opt.MapFrom(src => src.Store.Name))
                .ForMember(dest => dest.StoreImageUrl, opt => opt.MapFrom(src => src.Store.LogoUrl))
                .ForMember(dest => dest.CurrentPrice, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.SalePercent, opt => opt.MapFrom(src => src.SalePercent)) // Not available in entity
                .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => 
                    src.Ratings.Any() ? src.Ratings.Average(r => r.Rating) : 0))
                .ForMember(dest => dest.RatingCount, opt => opt.MapFrom(src => src.Ratings.Count))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Not available in entity
                .ForMember(dest => dest.IsFavorite, opt => opt.Ignore()); // Set in service layer            // Product Image mappings
            CreateMap<ProductImage, ProductImageDto>()
                .ForMember(dest => dest.IsMainImage, opt => opt.Ignore()) // Not available in entity
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedAt));// Product Price mappings
            CreateMap<ProductPrice, ProductPriceDto>()
                .ForMember(dest => dest.IsCurrentPrice, opt => opt.Ignore()) // Not available in entity
                .ForMember(dest => dest.DiscountedPrice, opt => opt.Ignore()) // Not available in entity
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedAt));            // Product Rating mappings
            CreateMap<ProductRating, ProductRatingDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name))
                .ForMember(dest => dest.Review, opt => opt.MapFrom(src => src.Comment))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedAt));
        }
    }
}
