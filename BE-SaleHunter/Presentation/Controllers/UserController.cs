using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Store;
using BE_SaleHunter.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BE_SaleHunter.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("profile")]
        public async Task<ActionResult<BaseResponseDto<UserDto>>> GetProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<UserDto>.Failure("Invalid user"));
                }

                var result = await _userService.GetUserProfileAsync(userId.Value);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, BaseResponseDto<UserDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get user by ID (Admin only)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseDto<UserDto>>> GetUser(long id)
        {
            try
            {
                var result = await _userService.GetUserByIdAsync(id);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                return StatusCode(500, BaseResponseDto<UserDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Update current user profile
        /// </summary>
        [HttpPut("profile")]
        public async Task<ActionResult<BaseResponseDto<UserDto>>> UpdateProfile([FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<UserDto>.Failure("Invalid user"));
                }

                var result = await _userService.UpdateUserAsync(userId.Value, updateUserDto);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, BaseResponseDto<UserDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponseDto<IEnumerable<UserDto>>>> GetAllUsers()
        {
            try
            {
                var result = await _userService.GetAllUsersAsync();
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, BaseResponseDto<IEnumerable<UserDto>>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Deactivate user (Admin only)
        /// </summary>
        [HttpPost("{id}/deactivate")]
        public async Task<ActionResult<BaseResponseDto<bool>>> DeactivateUser(long id)
        {
            try
            {
                var result = await _userService.DeactivateUserAsync(id);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", id);
                return StatusCode(500, BaseResponseDto<bool>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Activate user (Admin only)
        /// </summary>
        [HttpPost("{id}/activate")]
        public async Task<ActionResult<BaseResponseDto<bool>>> ActivateUser(long id)
        {
            try
            {
                var result = await _userService.ActivateUserAsync(id);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user: {UserId}", id);
                return StatusCode(500, BaseResponseDto<bool>.Failure("Internal server error"));
            }
        }
        /// <summary>
        /// Get customer analytics
        /// </summary>
        [HttpGet("analytics")]
        //[Authorize(Policy = "Administrator")]
        public async Task<ActionResult<BaseResponseDto<CustomerAnalyticsDto>>> GetCustomerAnalytics()
        {
            _logger.LogInformation("GetCustomerAnalytics request received");

            try
            {
                var result = await _userService.GetCustomerAnalyticsAsync();

                if (result.IsSuccess)
                {
                    _logger.LogInformation("GetCustomerAnalytics successful - TotalCustomers: {Total}", result.Data?.TotalCustomers ?? -1);
                    return Ok(result);
                }

                _logger.LogWarning("GetCustomerAnalytics failed - Error: {Error}", result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer analytics");
                return StatusCode(500, BaseResponseDto<CustomerAnalyticsDto>.Failure("Internal server error"));
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
