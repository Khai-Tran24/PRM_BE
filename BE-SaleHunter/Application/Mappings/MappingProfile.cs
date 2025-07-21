using AutoMapper;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Store;
using BE_SaleHunter.Application.DTOs.Chat;

namespace BE_SaleHunter.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.HasStore, opt => opt.MapFrom(src => src.HasStore()))
                .ForMember(dest => dest.AccountType, opt => opt.MapFrom(src => src.GetAccountType()))
                .ForMember(dest => dest.HasStore, opt => opt.MapFrom(src => src.Store != null))
                .ForMember(dest => dest.StoreId, opt => opt.MapFrom(src => src.Store != null ? src.Store.Id : 0));
            // Store mappings
            CreateMap<Store, StoreDto>()
                .ForMember(dest => dest.Products, opt => opt.MapFrom(src => src.Products))
                .ForMember(dest => dest.ProductsCount, opt => opt.MapFrom(src => src.Products.Count));
            // Product mappings
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
                .ForMember(dest => dest.StoreName, opt => opt.MapFrom(src => src.Store.Name))
                .ForMember(dest => dest.StoreImageUrl, opt => opt.MapFrom(src => src.Store.LogoUrl))
                .ForMember(dest => dest.CurrentPrice, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.SalePercent,
                    opt => opt.MapFrom(src => src.SalePercent)) // Not available in entity
                .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src =>
                    src.Ratings.Any() ? src.Ratings.Average(r => r.Rating) : 0))
                .ForMember(dest => dest.Prices, opt => opt.MapFrom(src => src.PriceHistory))
                .ForMember(dest => dest.RatingCount, opt => opt.MapFrom(src => src.Ratings.Count))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Not available in entity
                .ForMember(dest => dest.IsFavorite,
                    opt => opt.Ignore()) // Set in service layer            
                .ForMember(dest => dest.MainImage,
                    opt => opt.MapFrom(src =>
                        src.Images.Any()
                            ? (src.Images.FirstOrDefault(i => i.DisplayOrder == 0) != null
                                ? src.Images.FirstOrDefault(i => i.DisplayOrder == 0)!.ImageUrl
                                : src.Images.FirstOrDefault()!.ImageUrl)
                            : null))
                .ForMember(dest => dest.TotalViews, opt => opt.MapFrom(src => src.TotalViews));

            // Product Image mappings
            CreateMap<ProductImage, ProductImageDto>()
                .ForMember(dest => dest.IsMainImage, opt => opt.Ignore()) // Not available in entity
                .ForMember(dest => dest.CreatedDate,
                    opt => opt.MapFrom(src => src.CreatedAt));
            // Product Price mappings
            CreateMap<ProductPrice, ProductPriceDto>()
                .ForMember(dest => dest.IsCurrentPrice, opt => opt.Ignore()) // Not available in entity
                .ForMember(dest => dest.DiscountedPrice, opt => opt.Ignore()) // Not available in entity
                .ForMember(dest => dest.CreatedDate,
                    opt => opt.MapFrom(src => src.CreatedAt));
            // Product Rating mappings
            CreateMap<ProductRating, ProductRatingDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name))
                .ForMember(dest => dest.Review, opt => opt.MapFrom(src => src.Comment))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedAt));

            // Chat mappings
            CreateMap<ChatConversation, ChatConversationDto>()
                .ForMember(dest => dest.Messages, opt => opt.MapFrom(src => src.Messages));

            CreateMap<ChatMessage, ChatMessageDto>();
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.OrderDetail, opt => opt.MapFrom(src => src.OrderDetails))
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
                .ReverseMap();
            CreateMap<OrderDetail, OrderDetailDto>()
                .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product))
                .ReverseMap();
        }
    }
}