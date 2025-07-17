using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_SaleHunter.Application.Services;
using BE_SaleHunter.Application.DTOs;
using System.Security.Claims;

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
        public async Task<ActionResult<BaseResponseDto<ProductDto>>> CreateProduct([FromBody] CreateProductDto createProductDto)
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
                    // Record product view if user is authenticated
                    var userId = GetCurrentUserId();
                    if (userId.HasValue)
                    {
                        _ = Task.Run(async () => await _productService.AddProductViewAsync(id, userId.Value));
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
        public async Task<ActionResult<BaseResponseDto<ProductDto>>> UpdateProduct(long id, [FromBody] UpdateProductDto updateProductDto)
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
        /// Search products with filters
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<ProductDto>>>> SearchProducts(
            [FromQuery] string query = null,
            [FromQuery] long? storeId = null,
            [FromQuery] string? category = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null)
        {
            try
            {
                var result = await _productService.SearchProductsAsync(query, storeId, category, minPrice, maxPrice);
                
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
        public async Task<ActionResult<BaseResponseDto<bool>>> AddRating(long id, [FromBody] CreateProductRatingDto ratingDto)
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
