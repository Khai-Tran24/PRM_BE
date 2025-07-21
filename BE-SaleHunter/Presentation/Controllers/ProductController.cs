using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_SaleHunter.Application.Services;
using BE_SaleHunter.Application.DTOs;
using System.Security.Claims;
using BE_SaleHunter.Application.DTOs.Product;

namespace BE_SaleHunter.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new product (Store owners only)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<ProductDto>>> CreateProduct(
            [FromBody] CreateProductDto createProductDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<ProductDto>.Failure("Invalid user"));
                }

                var result = await _productService.CreateProductAsync(createProductDto, userId.Value);
                if (result.IsSuccess)
                {
                    return CreatedAtAction(nameof(GetProduct), new { id = result.Data?.Id }, result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, BaseResponseDto<ProductDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseDto<ProductDto>>> GetProduct(long id)
        {
            try
            {
                var result = await _productService.GetProductByIdAsync(id);

                if (result.IsSuccess)
                {
                    // Record product view if user is authenticated (fire-and-forget)
                    var userId = GetCurrentUserId();
                    if (userId.HasValue)
                    {
                        // Use Task.Factory.StartNew with TaskCreationOptions.LongRunning
                        // to avoid blocking the thread pool and create a new scope
                        _ = Task.Factory.StartNew(async () =>
                        {
                            try
                            {
                                using var scope = HttpContext.RequestServices.CreateScope();
                                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                                await productService.AddProductViewAsync(id, userId.Value);
                            }
                            catch (Exception ex)
                            {
                                // Log error but don't throw to avoid unhandled exceptions
                                var logger = HttpContext.RequestServices.GetService<ILogger<ProductController>>();
                                logger?.LogError(ex, "Error recording product view in background task");
                            }
                        }, TaskCreationOptions.LongRunning).Unwrap();
                    }

                    return Ok(result);
                }

                return NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by ID: {ProductId}", id);
                return StatusCode(500, BaseResponseDto<ProductDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Update product (Product owner only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<ProductDto>>> UpdateProduct(long id,
            [FromBody] UpdateProductDto updateProductDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<ProductDto>.Failure("Invalid user"));
                }

                var result = await _productService.UpdateProductAsync(id, updateProductDto, userId.Value);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", id);
                return StatusCode(500, BaseResponseDto<ProductDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Delete product (Product owner only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<bool>>> DeleteProduct(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<bool>.Failure("Invalid user"));
                }

                var result = await _productService.DeleteProductAsync(id, userId.Value);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                return StatusCode(500, BaseResponseDto<bool>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get products by store ID
        /// </summary>
        [HttpGet("store/{storeId}")]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<ProductDto>>>> GetProductsByStore(long storeId)
        {
            try
            {
                var result = await _productService.GetProductsByStoreIdAsync(storeId);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by store ID: {StoreId}", storeId);
                return StatusCode(500, BaseResponseDto<IEnumerable<ProductDto>>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Search products with filters and sorting
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<ProductDto>>>> SearchProducts(
            [FromQuery] int size,
            [FromQuery] int page,
            [FromQuery] string? query,
            [FromQuery] long? storeId = null,
            [FromQuery] string? category = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? sortBy = "popularity",
            [FromQuery] string? brand = null
        )
        {
            try
            {
                var pagingRequest = new PagingRequestDto()
                {
                    Size = size > 0 ? size : 10, // Default to 10 if size is less than or equal to 0
                    Page = page >= 0 ? page : 0 // Default to 0 if page is less than 0
                };
                var searchRequest = new ProductSearchRequestDto()
                {
                    Query = query,
                    StoreId = storeId,
                    Category = category,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    SortBy = sortBy ?? "popularity",
                    Brand = brand
                };
                var result = await _productService.SearchProductsAsync(
                    searchRequest, pagingRequest);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with query: {Query}", query);
                return StatusCode(500, BaseResponseDto<IEnumerable<ProductDto>>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get user's favorite products
        /// </summary>
        [HttpGet("favorites")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<ProductDto>>>> GetFavorites()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<IEnumerable<ProductDto>>.Failure("Invalid user"));
                }

                var result = await _productService.GetFavoriteProductsAsync(userId.Value);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorite products");
                return StatusCode(500, BaseResponseDto<IEnumerable<ProductDto>>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Add product to favorites
        /// </summary>
        [HttpPost("{id}/favorite")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<bool>>> AddToFavorites(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<bool>.Failure("Invalid user"));
                }

                var result = await _productService.AddToFavoritesAsync(id, userId.Value);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to favorites: {ProductId}", id);
                return StatusCode(500, BaseResponseDto<bool>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Remove product from favorites
        /// </summary>
        [HttpDelete("{id}/favorite")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<bool>>> RemoveFromFavorites(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<bool>.Failure("Invalid user"));
                }

                var result = await _productService.RemoveFromFavoritesAsync(id, userId.Value);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product from favorites: {ProductId}", id);
                return StatusCode(500, BaseResponseDto<bool>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get user's view history
        /// </summary>
        [HttpGet("history")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<ProductDto>>>> GetViewHistory()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<IEnumerable<ProductDto>>.Failure("Invalid user"));
                }

                var result = await _productService.GetViewHistoryAsync(userId.Value);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting view history");
                return StatusCode(500, BaseResponseDto<IEnumerable<ProductDto>>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Add/Update product rating
        /// </summary>
        [HttpPost("{id}/rating")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<bool>>> AddRating(long id,
            [FromBody] CreateProductRatingDto ratingDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<bool>.Failure("Invalid user"));
                }

                var result = await _productService.AddProductRatingAsync(id, userId.Value, ratingDto);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product rating: {ProductId}", id);
                return StatusCode(500, BaseResponseDto<bool>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get product ratings
        /// </summary>
        [HttpGet("{id}/ratings")]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<ProductRatingDto>>>> GetRatings(long id)
        {
            try
            {
                var result = await _productService.GetProductRatingsAsync(id);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product ratings: {ProductId}", id);
                return StatusCode(500, BaseResponseDto<IEnumerable<ProductRatingDto>>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get recommended products for user
        /// </summary>
        [HttpGet("recommended")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<ProductDto>>>> GetRecommendedProducts()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<IEnumerable<ProductDto>>.Failure("Invalid user"));
                }

                var result = await _productService.GetRecommendedProductsAsync(userId.Value);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommended products");
                return StatusCode(500, BaseResponseDto<IEnumerable<ProductDto>>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get products currently on sale
        /// </summary>
        [HttpGet("on-sale")]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<ProductDto>>>> GetOnSaleProducts()
        {
            try
            {
                var result = await _productService.GetOnSaleProductsAsync();

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting on-sale products");
                return StatusCode(500, BaseResponseDto<IEnumerable<ProductDto>>.Failure("Internal server error"));
            }
        }

        private long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }
    }
}