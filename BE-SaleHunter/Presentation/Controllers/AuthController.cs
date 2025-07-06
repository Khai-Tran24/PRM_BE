using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_SaleHunter.Application.Services;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Auth;
using System.Security.Claims;

namespace BE_SaleHunter.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// User login with email and password
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<BaseResponseDto<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in login endpoint");
                return StatusCode(500, BaseResponseDto<LoginResponseDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// User registration with email and password
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<BaseResponseDto<RegisterResponseDto>>> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in register endpoint");
                return StatusCode(500, BaseResponseDto<RegisterResponseDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<BaseResponseDto<string>>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in refresh token endpoint");
                return StatusCode(500, BaseResponseDto<string>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// User logout - invalidates refresh token
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<bool>>> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(BaseResponseDto<bool>.Failure("Invalid user"));
                }

                var result = await _authService.LogoutAsync(userId);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in logout endpoint");
                return StatusCode(500, BaseResponseDto<bool>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<BaseResponseDto<bool>>> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(BaseResponseDto<bool>.Failure("Invalid user"));
                }

                var result = await _authService.ChangePasswordAsync(userId, request);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in change password endpoint");
                return StatusCode(500, BaseResponseDto<bool>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Request password reset
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<BaseResponseDto<bool>>> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                var result = await _authService.ForgotPasswordAsync(request);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password endpoint");
                return StatusCode(500, BaseResponseDto<bool>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Reset password using reset token
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<ActionResult<BaseResponseDto<bool>>> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(request);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reset password endpoint");
                return StatusCode(500, BaseResponseDto<bool>.Failure("Internal server error"));
            }
        }
    }
}
