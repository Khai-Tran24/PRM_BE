using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_SaleHunter.Application.Services;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Store;
using System.Security.Claims;

namespace BE_SaleHunter.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreController : ControllerBase
    {
        private readonly IStoreService _storeService;
        private readonly ILogger<StoreController> _logger;

        public StoreController(IStoreService storeService, ILogger<StoreController> logger)
        {
            _storeService = storeService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new store (Authenticated users only)
        /// </summary>        [HttpPost]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<StoreDto>>> CreateStore([FromBody] CreateStoreDto createStoreDto)
        {
            _logger.LogInformation("CreateStore request received - User: {UserId}, Store Name: {StoreName}, Category: {Category}", 
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value, createStoreDto.Name, createStoreDto.Category);
            
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning("CreateStore failed - Invalid user token");
                    return BadRequest(BaseResponseDto<StoreDto>.Failure("Invalid user"));
                }

                _logger.LogDebug("CreateStore - Calling service layer for UserId: {UserId}", userId.Value);
                var result = await _storeService.CreateStoreAsync(createStoreDto, userId.Value);                  if (result.IsSuccess)
                {
                    _logger.LogInformation("CreateStore successful - StoreId: {StoreId}, UserId: {UserId}", result.Data?.Id, userId.Value);
                    return CreatedAtAction(nameof(GetStore), new { id = result.Data?.Id }, result);
                }
                
                _logger.LogWarning("CreateStore failed - UserId: {UserId}, Error: {Error}", userId.Value, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating store");
                return StatusCode(500, BaseResponseDto<StoreDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get store by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseDto<StoreDto>>> GetStore(long id)
        {
            try
            {
                var result = await _storeService.GetStoreByIdAsync(id);
                
                if (result.IsSuccess)
                {
                    _logger.LogDebug("GetStore successful - StoreId: {StoreId}, StoreName: {StoreName}", 
                        id, result.Data?.Name);
                    return Ok(result);
                }
                
                _logger.LogInformation("GetStore not found - StoreId: {StoreId}", id);
                return NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting store by ID: {StoreId}", id);
                return StatusCode(500, BaseResponseDto<StoreDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get current user's store
        /// </summary>
        [HttpGet("my-store")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<StoreDto>>> GetMyStore()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<StoreDto>.Failure("Invalid user"));
                }

                var result = await _storeService.GetStoreByUserIdAsync(userId.Value);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user's store");
                return StatusCode(500, BaseResponseDto<StoreDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Update store (Store owner only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]        public async Task<ActionResult<BaseResponseDto<StoreDto>>> UpdateStore(long id,
            [FromBody] UpdateStoreDto updateStoreDto)
        {
            _logger.LogInformation("UpdateStore request received - StoreId: {StoreId}, UserId: {UserId}, UpdateFields: {@UpdateFields}", 
                id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value, updateStoreDto);
            
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning("UpdateStore failed - Invalid user token for StoreId: {StoreId}", id);
                    return BadRequest(BaseResponseDto<StoreDto>.Failure("Invalid user"));
                }

                var result = await _storeService.UpdateStoreAsync(id, updateStoreDto, userId.Value);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("UpdateStore successful - StoreId: {StoreId}, UserId: {UserId}, UpdatedStoreName: {StoreName}", 
                        id, userId.Value, result.Data?.Name);
                    return Ok(result);
                }

                _logger.LogWarning("UpdateStore failed - StoreId: {StoreId}, UserId: {UserId}, Error: {Error}", 
                    id, userId.Value, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store: {StoreId}", id);
                return StatusCode(500, BaseResponseDto<StoreDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Delete store (Store owner only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<bool>>> DeleteStore(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<bool>.Failure("Invalid user"));
                }

                var result = await _storeService.DeleteStoreAsync(id, userId.Value);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting store: {StoreId}", id);
                return StatusCode(500, BaseResponseDto<bool>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get all stores
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<StoreDto>>>> GetAllStores()
        {
            try
            {
                var result = await _storeService.GetAllStoresAsync();

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all stores");
                return StatusCode(500, BaseResponseDto<IEnumerable<StoreDto>>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Search stores by name or location
        /// </summary>        [HttpGet("search")]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<StoreDto>>>> SearchStores(
            [FromQuery] string query,
            [FromQuery] decimal? latitude = null,
            [FromQuery] decimal? longitude = null,
            [FromQuery] double? radiusKm = null)
        {
            _logger.LogInformation("SearchStores request received - Query: '{Query}', Location: ({Lat}, {Lng}), Radius: {Radius}km", 
                query, latitude, longitude, radiusKm);
            
            try
            {
                var result = await _storeService.SearchStoresAsync(query, latitude, longitude, radiusKm);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("SearchStores successful - Found {StoreCount} stores for query: '{Query}'", 
                        result.Data?.Count() ?? 0, query);
                    return Ok(result);
                }
                
                _logger.LogWarning("SearchStores failed - Query: '{Query}', Error: {Error}", query, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching stores with query: {Query}", query);
                return StatusCode(500, BaseResponseDto<IEnumerable<StoreDto>>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get nearby stores based on location
        /// </summary>
        [HttpGet("nearby")]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<StoreDto>>>> GetNearbyStores(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude,
            [FromQuery] double radiusKm = 10)
        {
            try
            {
                var result = await _storeService.GetNearbyStoresAsync(latitude, longitude, radiusKm);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearby stores at coordinates: {Latitude}, {Longitude}", latitude,
                    longitude);
                return StatusCode(500, BaseResponseDto<IEnumerable<StoreDto>>.Failure("Internal server error"));
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