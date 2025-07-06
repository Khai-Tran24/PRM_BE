using System.ComponentModel.DataAnnotations;

namespace BE_SaleHunter.Application.DTOs.Auth
{
    // Login DTOs
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }    // Register DTOs
    public class RegisterRequestDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class RegisterResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
    }

    // Change Password DTOs
    public class ChangePasswordRequestDto
    {
        [Required]  
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    // Forgot Password DTOs
    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    // Refresh Token DTOs
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    // Legacy DTOs for backward compatibility
    public class SignInDto : LoginRequestDto { }
    public class SignUpDto : RegisterRequestDto { }
    public class AuthResponseDto : LoginResponseDto { }
    public class ChangePasswordDto : ChangePasswordRequestDto { }
    public class ResetPasswordDto : ForgotPasswordRequestDto { }
    public class ResetPasswordConfirmDto : ResetPasswordRequestDto { }

    // Social Auth DTOs
    public class GoogleAuthDto
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }

    public class FacebookAuthDto
    {
        [Required]
        public string AccessToken { get; set; } = string.Empty;
    }
}
