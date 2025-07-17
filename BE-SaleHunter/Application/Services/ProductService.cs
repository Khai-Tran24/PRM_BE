using AutoMapper;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Application.DTOs;

namespace BE_SaleHunter.Application.Services
{
    public interface IProductService
    {
        Task<BaseResponseDto<ProductDto>> CreateProductAsync(CreateProductDto createProductDto, long userId);
        Task<BaseResponseDto<ProductDto>> GetProductByIdAsync(long productId);

        Task<BaseResponseDto<ProductDto>> UpdateProductAsync(long productId, UpdateProductDto updateProductDto,
            long userId);

        Task<BaseResponseDto<bool>> DeleteProductAsync(long productId, long userId);
        Task<BaseResponseDto<IEnumerable<ProductDto>>> GetProductsByStoreIdAsync(long storeId);

        Task<BaseResponseDto<IEnumerable<ProductDto>>> SearchProductsAsync(string query, long? storeId = null,
            string? category = null, decimal? minPrice = null, decimal? maxPrice = null);

        Task<BaseResponseDto<IEnumerable<ProductDto>>> GetFavoriteProductsAsync(long userId);
        Task<BaseResponseDto<bool>> AddToFavoritesAsync(long productId, long userId);
        Task<BaseResponseDto<bool>> RemoveFromFavoritesAsync(long productId, long userId);
        Task<BaseResponseDto<bool>> AddProductViewAsync(long productId, long userId);
        Task<BaseResponseDto<IEnumerable<ProductDto>>> GetViewHistoryAsync(long userId);
        Task<BaseResponseDto<IEnumerable<ProductDto>>> GetRecommendedProductsAsync(long userId);
        Task<BaseResponseDto<IEnumerable<ProductDto>>> GetOnSaleProductsAsync();

        Task<BaseResponseDto<bool>>
            AddProductRatingAsync(long productId, long userId, CreateProductRatingDto ratingDto);

        Task<BaseResponseDto<IEnumerable<ProductRatingDto>>> GetProductRatingsAsync(long productId);
    }

    public class ProductService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ProductService> logger,
        IImageStorageService imageStorageService)
        : IProductService
    {
        public async Task<BaseResponseDto<ProductDto>> CreateProductAsync(CreateProductDto createProductDto,
            long userId)
        {
            try
            {
                // Verify user owns a store
                var user = await unitOfWork.UserRepository.GetByIdWithStoreAsync(userId);
                if (user?.Store == null)
                {
                    return BaseResponseDto<ProductDto>.Failure("You must have a store to create products");
                }

                var product = new Product
                {
                    Name = createProductDto.Name,
                    Description = createProductDto.Description,
                    Category = createProductDto.Category,
                    Brand = createProductDto.Brand,
                    Price = createProductDto.Price,
                    SalePercent = createProductDto.SalePercent ?? 0,
                    StoreId = user.Store.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await unitOfWork.ProductRepository.AddAsync(product);
                await unitOfWork.CompleteAsync(); // Add product price history
                var productPrice = new ProductPrice
                {
                    ProductId = product.Id,
                    Price = createProductDto.Price,
                    CreatedAt = DateTime.UtcNow
                };

                await unitOfWork.GenericRepository<ProductPrice>().AddAsync(productPrice);

                // Handle product images
                if (createProductDto.Images.Any())
                {
                    var productImages = new List<ProductImage>();
                    for (int i = 0; i < createProductDto.Images.Count; i++)
                    {
                        try
                        {
                            var imageUrl = await imageStorageService.UploadBase64ImageAsync(
                                createProductDto.Images[i],
                                $"products/{product.Id}/image-{i}-{DateTime.UtcNow.Ticks}");
                            productImages.Add(new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl = imageUrl,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to upload image {Index} for product {ProductId}", i,
                                product.Id);
                        }
                    }

                    if (productImages.Any())
                    {
                        foreach (var image in productImages)
                        {
                            await unitOfWork.GenericRepository<ProductImage>().AddAsync(image);
                        }
                    }
                }

                await unitOfWork.CompleteAsync();

                // Reload product with related data
                var createdProduct = await unitOfWork.ProductRepository.GetByIdWithAllDetailsAsync(product.Id);
                var productDto = mapper.Map<ProductDto>(createdProduct);

                return BaseResponseDto<ProductDto>.Success(productDto, "Product created successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating product for user: {UserId}", userId);
                return BaseResponseDto<ProductDto>.Failure("An error occurred while creating the product");
            }
        }

        public async Task<BaseResponseDto<ProductDto>> GetProductByIdAsync(long productId)
        {
            try
            {
                var product = await unitOfWork.ProductRepository.GetByIdWithAllDetailsAsync(productId);
                if (product == null)
                {
                    return BaseResponseDto<ProductDto>.Failure("Product not found");
                }

                var productDto = mapper.Map<ProductDto>(product);
                return BaseResponseDto<ProductDto>.Success(productDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting product by ID: {ProductId}", productId);
                return BaseResponseDto<ProductDto>.Failure("An error occurred while retrieving the product");
            }
        }

        public async Task<BaseResponseDto<ProductDto>> UpdateProductAsync(long productId,
            UpdateProductDto updateProductDto, long userId)
        {
            try
            {
                var product = await unitOfWork.ProductRepository.GetByIdWithStoreAsync(productId);
                if (product == null)
                {
                    return BaseResponseDto<ProductDto>.Failure("Product not found");
                }

                if (product.Store.UserId != userId)
                {
                    return BaseResponseDto<ProductDto>.Failure("You are not authorized to update this product");
                }

                // Update product properties
                if (!string.IsNullOrWhiteSpace(updateProductDto.Name))
                {
                    product.Name = updateProductDto.Name;
                }

                if (!string.IsNullOrWhiteSpace(updateProductDto.Description))
                {
                    product.Description = updateProductDto.Description;
                }

                if (!string.IsNullOrWhiteSpace(updateProductDto.Category))
                {
                    product.Category = updateProductDto.Category;
                }

                if (!string.IsNullOrWhiteSpace(updateProductDto.Brand))
                {
                    product.Brand = updateProductDto.Brand;
                }

                if (updateProductDto.Price.HasValue)
                {
                    product.Price = updateProductDto.Price.Value;
                }
                if (updateProductDto.Price.HasValue)
                {
                    product.SalePercent = updateProductDto.SalePercent.Value;
                }
                product.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.ProductRepository.UpdateAsync(product); // Update price history if provided
                if (updateProductDto.Price.HasValue)
                {
                    // Add new price to history
                    var newPrice = new ProductPrice
                    {
                        ProductId = productId,
                        Price = updateProductDto.Price.Value,
                        CreatedAt = DateTime.UtcNow
                    };

                    await unitOfWork.GenericRepository<ProductPrice>().AddAsync(newPrice);
                }

                // Handle new images
                if (updateProductDto.NewImages.Any())
                {
                    for (int i = 0; i < updateProductDto.NewImages.Count; i++)
                    {
                        try
                        {
                            var imageUrl = await imageStorageService.UploadBase64ImageAsync(
                                updateProductDto.NewImages[i],
                                $"products/{productId}/update-{i}-{DateTime.UtcNow.Ticks}");
                            var productImage = new ProductImage
                            {
                                ProductId = productId,
                                ImageUrl = imageUrl,
                                CreatedAt = DateTime.UtcNow
                            };

                            await unitOfWork.GenericRepository<ProductImage>().AddAsync(productImage);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to upload new image {Index} for product {ProductId}", i,
                                productId);
                        }
                    }
                }

                await unitOfWork.CompleteAsync();

                // Reload product with updated data
                var updatedProduct = await unitOfWork.ProductRepository.GetByIdWithAllDetailsAsync(productId);
                var productDto = mapper.Map<ProductDto>(updatedProduct);

                return BaseResponseDto<ProductDto>.Success(productDto, "Product updated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating product: {ProductId}", productId);
                return BaseResponseDto<ProductDto>.Failure("An error occurred while updating the product");
            }
        }

        public async Task<BaseResponseDto<bool>> DeleteProductAsync(long productId, long userId)
        {
            try
            {
                var product = await unitOfWork.ProductRepository.GetByIdWithStoreAsync(productId);
                if (product == null)
                {
                    return BaseResponseDto<bool>.Failure("Product not found");
                }

                if (product.Store.UserId != userId)
                {
                    return BaseResponseDto<bool>.Failure("You are not authorized to delete this product");
                }

                // Hard delete (or you could implement soft delete by adding an IsDeleted field)
                await unitOfWork.ProductRepository.DeleteAsync(product);
                await unitOfWork.CompleteAsync();

                return BaseResponseDto<bool>.Success(true, "Product deleted successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting product: {ProductId}", productId);
                return BaseResponseDto<bool>.Failure("An error occurred while deleting the product");
            }
        }

        public async Task<BaseResponseDto<IEnumerable<ProductDto>>> GetProductsByStoreIdAsync(long storeId)
        {
            try
            {
                var products = await unitOfWork.ProductRepository.GetByStoreIdAsync(storeId);
                var productDtos = mapper.Map<IEnumerable<ProductDto>>(products);
                return BaseResponseDto<IEnumerable<ProductDto>>.Success(productDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting products by store ID: {StoreId}", storeId);
                return BaseResponseDto<IEnumerable<ProductDto>>.Failure("An error occurred while retrieving products");
            }
        }

        public async Task<BaseResponseDto<IEnumerable<ProductDto>>> SearchProductsAsync(string query,
            long? storeId = null, string? category = null, decimal? minPrice = null, decimal? maxPrice = null)
        {
            try
            {
                var products =
                    await unitOfWork.ProductRepository.SearchProductsAsync(query, storeId, category, minPrice,
                        maxPrice);
                var productDtos = mapper.Map<IEnumerable<ProductDto>>(products);
                return BaseResponseDto<IEnumerable<ProductDto>>.Success(productDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error searching products with query: {Query}", query);
                return BaseResponseDto<IEnumerable<ProductDto>>.Failure("An error occurred while searching products");
            }
        }

        public async Task<BaseResponseDto<IEnumerable<ProductDto>>> GetFavoriteProductsAsync(long userId)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetUserWithFavoritesAsync(userId);
                if (user == null)
                {
                    return BaseResponseDto<IEnumerable<ProductDto>>.Failure("User not found");
                }

                var products = user.Favorites.Select(f => f.Product).ToList();
                var productDtos = mapper.Map<IEnumerable<ProductDto>>(products);
                return BaseResponseDto<IEnumerable<ProductDto>>.Success(productDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting favorite products for user: {UserId}", userId);
                return BaseResponseDto<IEnumerable<ProductDto>>.Failure(
                    "An error occurred while retrieving favorite products");
            }
        }

        public async Task<BaseResponseDto<bool>> AddToFavoritesAsync(long productId, long userId)
        {
            try
            {
                // Check if already in favorites
                var existingFavorite = await unitOfWork.GenericRepository<UserFavorite>()
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

                if (existingFavorite != null)
                {
                    return BaseResponseDto<bool>.Success(true, "Product is already in favorites");
                }

                var favorite = new UserFavorite
                {
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };

                await unitOfWork.GenericRepository<UserFavorite>().AddAsync(favorite);
                await unitOfWork.CompleteAsync();

                return BaseResponseDto<bool>.Success(true, "Product added to favorites");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding product to favorites: {ProductId}, User: {UserId}", productId,
                    userId);
                return BaseResponseDto<bool>.Failure("An error occurred while adding product to favorites");
            }
        }

        public async Task<BaseResponseDto<bool>> RemoveFromFavoritesAsync(long productId, long userId)
        {
            try
            {
                var favorite = await unitOfWork.GenericRepository<UserFavorite>()
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

                if (favorite == null)
                {
                    return BaseResponseDto<bool>.Success(true, "Product is not in favorites");
                }

                await unitOfWork.GenericRepository<UserFavorite>().DeleteAsync(favorite);
                await unitOfWork.CompleteAsync();

                return BaseResponseDto<bool>.Success(true, "Product removed from favorites");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing product from favorites: {ProductId}, User: {UserId}", productId,
                    userId);
                return BaseResponseDto<bool>.Failure("An error occurred while removing product from favorites");
            }
        }

        public async Task<BaseResponseDto<bool>> AddProductViewAsync(long productId, long userId)
        {
            try
            {
                var productView = new ProductView
                {
                    UserId = userId,
                    ProductId = productId,
                    ViewedAt = DateTime.UtcNow
                };

                await unitOfWork.GenericRepository<ProductView>().AddAsync(productView);
                await unitOfWork.CompleteAsync();

                return BaseResponseDto<bool>.Success(true, "Product view recorded");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding product view: {ProductId}, User: {UserId}", productId, userId);
                return BaseResponseDto<bool>.Success(true,
                    "Product view not recorded, but continuing"); // Don't fail for view tracking
            }
        }

        public async Task<BaseResponseDto<IEnumerable<ProductDto>>> GetViewHistoryAsync(long userId)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetUserWithViewHistoryAsync(userId);
                if (user == null)
                {
                    return BaseResponseDto<IEnumerable<ProductDto>>.Failure("User not found");
                }

                var products = user.ProductViews
                    .OrderByDescending(pv => pv.ViewedAt)
                    .Select(pv => pv.Product)
                    .Distinct()
                    .Take(50) // Limit to recent 50 products
                    .ToList();

                var productDtos = mapper.Map<IEnumerable<ProductDto>>(products);
                return BaseResponseDto<IEnumerable<ProductDto>>.Success(productDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting view history for user: {UserId}", userId);
                return BaseResponseDto<IEnumerable<ProductDto>>.Failure(
                    "An error occurred while retrieving view history");
            }
        }

        public async Task<BaseResponseDto<bool>> AddProductRatingAsync(long productId, long userId,
            CreateProductRatingDto ratingDto)
        {
            try
            {
                // Check if user already rated this product
                var existingRating =
                    await unitOfWork.ProductRatingRepository.GetUserRatingForProductAsync(productId, userId);
                if (existingRating != null)
                {
                    // Update existing rating
                    existingRating.Rating = ratingDto.Rating;
                    existingRating.Comment = ratingDto.Comment;
                    existingRating.UpdatedAt = DateTime.UtcNow;
                    await unitOfWork.ProductRatingRepository.UpdateAsync(existingRating);
                }
                else
                {
                    // Create new rating
                    var rating = new ProductRating
                    {
                        ProductId = productId,
                        UserId = userId,
                        Rating = ratingDto.Rating,
                        Comment = ratingDto.Comment,
                        CreatedAt = DateTime.UtcNow
                    };

                    await unitOfWork.ProductRatingRepository.AddAsync(rating);
                }

                await unitOfWork.CompleteAsync();
                return BaseResponseDto<bool>.Success(true, "Product rating added successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding product rating: {ProductId}, User: {UserId}", productId, userId);
                return BaseResponseDto<bool>.Failure("An error occurred while adding product rating");
            }
        }

        public async Task<BaseResponseDto<IEnumerable<ProductRatingDto>>> GetProductRatingsAsync(long productId)
        {
            try
            {
                var ratings = await unitOfWork.ProductRatingRepository.GetProductRatingsAsync(productId);
                var ratingDtos = mapper.Map<IEnumerable<ProductRatingDto>>(ratings);
                return BaseResponseDto<IEnumerable<ProductRatingDto>>.Success(ratingDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting product ratings: {ProductId}", productId);
                return BaseResponseDto<IEnumerable<ProductRatingDto>>.Failure(
                    "An error occurred while retrieving product ratings");
            }
        }

        public async Task<BaseResponseDto<IEnumerable<ProductDto>>> GetRecommendedProductsAsync(long userId)
        {
            try
            {
                // Simple implementation: return recent products from user's favorite stores
                // In a real implementation, this would use ML algorithms
                var userFavoriteRepo = unitOfWork.GenericRepository<UserFavorite>();
                var userFavorites = await userFavoriteRepo.GetAllAsync();
                var userSpecificFavorites = userFavorites.Where(f => f.UserId == userId).ToList();
                
                if (!userSpecificFavorites.Any())
                {
                    // If no favorites, return recent products
                    var recentProducts = await unitOfWork.ProductRepository.GetRecentProductsAsync(20);
                    var recentDtos = mapper.Map<IEnumerable<ProductDto>>(recentProducts);
                    return BaseResponseDto<IEnumerable<ProductDto>>.Success(recentDtos);
                }

                // Get products from the user's favorited product list
                var favoriteProductIds = userSpecificFavorites.Select(f => f.ProductId).ToList();
                var favoriteProducts = await unitOfWork.ProductRepository.GetAllAsync();
                var recommendedProducts = favoriteProducts.Where(p => favoriteProductIds.Contains(p.Id)).Take(20);
                var productDtos = mapper.Map<IEnumerable<ProductDto>>(recommendedProducts);
                return BaseResponseDto<IEnumerable<ProductDto>>.Success(productDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting recommended products for user: {UserId}", userId);
                return BaseResponseDto<IEnumerable<ProductDto>>.Failure("An error occurred while retrieving recommended products");
            }
        }

        public async Task<BaseResponseDto<IEnumerable<ProductDto>>> GetOnSaleProductsAsync()
        {
            try
            {
                var onSaleProducts = await unitOfWork.ProductRepository.GetOnSaleProductsAsync(50);
                var productDtos = mapper.Map<IEnumerable<ProductDto>>(onSaleProducts);
                return BaseResponseDto<IEnumerable<ProductDto>>.Success(productDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting on-sale products");
                return BaseResponseDto<IEnumerable<ProductDto>>.Failure("An error occurred while retrieving on-sale products");
            }
        }
    }
}