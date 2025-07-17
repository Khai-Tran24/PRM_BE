using AutoMapper;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Auth;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BE_SaleHunter.Application.Services
{
    public interface IAuthService
    {
        Task<BaseResponseDto<LoginResponseDto>> LoginAsync(LoginRequestDto request);
        Task<BaseResponseDto<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
        Task<BaseResponseDto<string>> RefreshTokenAsync(string refreshToken);
        Task<BaseResponseDto<bool>> LogoutAsync(string userId);
        Task<BaseResponseDto<bool>> ChangePasswordAsync(string userId, ChangePasswordRequestDto request);
        Task<BaseResponseDto<bool>> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<BaseResponseDto<bool>> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<BaseResponseDto<bool>> VerifyResetTokenAsync(string token);
    }

    public class AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<User> passwordHasher,
        IMapper mapper,
        IConfiguration configuration,
        ILogger<AuthService> logger, IEmailService _emailService)
        : IAuthService
    {
        public async Task<BaseResponseDto<LoginResponseDto>> LoginAsync(LoginRequestDto request)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    return BaseResponseDto<LoginResponseDto>.Failure("Invalid email or password");
                }

                var passwordResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
                if (passwordResult == PasswordVerificationResult.Failed)
                {
                    return BaseResponseDto<LoginResponseDto>.Failure("Invalid email or password");
                } // Update last login
                if (!user.IsActive)
                {
                    return BaseResponseDto<LoginResponseDto>.Failure("user was deactivated");
                }
                user.LastLoginDate = DateTime.UtcNow;


                var tokenResponse = GenerateTokens(user);

                var response = new LoginResponseDto
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    ExpiresAt = tokenResponse.ExpiresAt,
                    User = mapper.Map<UserDto>(user)
                };
                user.RefreshToken = tokenResponse.RefreshToken;
                user.RefreshTokenExpiry = tokenResponse.ExpiresAt;
                await unitOfWork.UserRepository.UpdateAsync(user);
                await unitOfWork.CompleteAsync();
                return BaseResponseDto<LoginResponseDto>.Success(response, "Login successful");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return BaseResponseDto<LoginResponseDto>.Failure("An error occurred during login");
            }
        }

        public async Task<BaseResponseDto<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await unitOfWork.UserRepository.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BaseResponseDto<RegisterResponseDto>.Failure("User with this email already exists");
                } // Create new user

                var user = new User
                {
                    Email = request.Email,
                    Name = request.Name,
                    PhoneNumber = request.PhoneNumber,
                    IsActive = true,
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow,
                    LastLoginDate = DateTime.UtcNow
                };

                // Hash password
                user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
                var tokenResponse = GenerateTokens(user);

                var response = new RegisterResponseDto
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    ExpiresAt = tokenResponse.ExpiresAt,
                    User = mapper.Map<UserDto>(user)
                };
                user.RefreshToken = tokenResponse.RefreshToken;
                user.RefreshTokenExpiry = tokenResponse.ExpiresAt;
                // Add user to database
                await unitOfWork.UserRepository.AddAsync(user);
                await unitOfWork.CompleteAsync();
                
                return BaseResponseDto<RegisterResponseDto>.Success(response, "Registration successful");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
                return BaseResponseDto<RegisterResponseDto>.Failure("An error occurred during registration");
            }
        }

        public async Task<BaseResponseDto<string>> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByRefreshTokenAsync(refreshToken);
                if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                {
                    return BaseResponseDto<string>.Failure("Invalid or expired refresh token");
                }

                var tokenResponse = GenerateTokens(user);
                return BaseResponseDto<string>.Success(tokenResponse.AccessToken, "Token refreshed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during token refresh");
                return BaseResponseDto<string>.Failure("An error occurred during token refresh");
            }
        }

        public async Task<BaseResponseDto<bool>> LogoutAsync(string userId)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByIdAsync(long.Parse(userId));
                if (user == null)
                {
                    return BaseResponseDto<bool>.Failure("User not found");
                } // Clear refresh token

                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await unitOfWork.UserRepository.UpdateAsync(user);
                await unitOfWork.CompleteAsync();

                return BaseResponseDto<bool>.Success(true, "Logout successful");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during logout for user: {UserId}", userId);
                return BaseResponseDto<bool>.Failure("An error occurred during logout");
            }
        }

        public async Task<BaseResponseDto<bool>> ChangePasswordAsync(string userId, ChangePasswordRequestDto request)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByIdAsync(long.Parse(userId));
                if (user == null)
                {
                    return BaseResponseDto<bool>.Failure("User not found");
                }

                var passwordResult =
                    passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
                if (passwordResult == PasswordVerificationResult.Failed)
                {
                    return BaseResponseDto<bool>.Failure("Current password is incorrect");
                }

                user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await unitOfWork.UserRepository.UpdateAsync(user);
                await unitOfWork.CompleteAsync();

                return BaseResponseDto<bool>.Success(true, "Password changed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during password change for user: {UserId}", userId);
                return BaseResponseDto<bool>.Failure("An error occurred while changing password");
            }
        }

        public async Task<BaseResponseDto<bool>> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    // Không tiết lộ user tồn tại hay không
                    return BaseResponseDto<bool>.Success(true,
                        "If an account with this email exists, a password reset link has been sent");
                }

                // Tạo token reset
                user.PasswordResetToken = Guid.NewGuid().ToString();
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                user.UpdatedAt = DateTime.UtcNow;

                await unitOfWork.UserRepository.UpdateAsync(user);
                await unitOfWork.CompleteAsync();

                // Tạo link reset
                var resetLink = $"{configuration["App:BaseUrl"]}ResetPassword?token={user.PasswordResetToken}";

                var htmlMessage = $@"
            <p>Hello,</p>
            <p>We received a request to reset your password. Click the link below to reset it:</p>
            <p><a href='{resetLink}'>Reset Password</a></p>
            <p>This link will expire in 1 hour.</p>
            <p>If you did not request this, please ignore this email.</p>";

                await _emailService.SendEmailAsync(user.Email, "Reset Your Password", htmlMessage);

                logger.LogInformation("Password reset email sent to {Email}", request.Email);

                return BaseResponseDto<bool>.Success(true,
                    "If an account with this email exists, a password reset link has been sent");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during password reset request for email: {Email}", request.Email);
                return BaseResponseDto<bool>.Failure("An error occurred while processing password reset request");
            }
        }

        public async Task<BaseResponseDto<bool>> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByPasswordResetTokenAsync(request.Token);
                if (user == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
                {
                    return BaseResponseDto<bool>.Failure("Invalid or expired reset token");
                }

                user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                user.UpdatedAt = DateTime.UtcNow;

                await unitOfWork.UserRepository.UpdateAsync(user);
                await unitOfWork.CompleteAsync();

                return BaseResponseDto<bool>.Success(true, "Password reset successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during password reset");
                return BaseResponseDto<bool>.Failure("An error occurred while resetting password");
            }
        }        
        private TokenResponseDto GenerateTokens(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Name),
                new("Role", user.Role),
                new("jti", Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(configuration["Jwt:ExpiryMinutes"])),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = configuration["Jwt:Issuer"],
                Audience = configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            // Generate refresh token
            var refreshToken = Guid.NewGuid().ToString();
            var refreshTokenExpiry =
                DateTime.UtcNow.AddDays(
                    Convert.ToDouble(configuration["Jwt:RefreshTokenExpiryDays"])); // Update user with refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = refreshTokenExpiry;
            user.UpdatedAt = DateTime.UtcNow;

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = tokenDescriptor.Expires.Value
            };
        }

        public async Task<BaseResponseDto<bool>> VerifyResetTokenAsync(string token)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByPasswordResetTokenAsync(token);
                if (user == null)
                {
                    return BaseResponseDto<bool>.Failure("Invalid or expired reset token");
                }

                if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
                {
                    return BaseResponseDto<bool>.Failure("Reset token has expired");
                }

                return BaseResponseDto<bool>.Success(true, "Reset token is valid");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error verifying reset token");
                return BaseResponseDto<bool>.Failure("An error occurred while verifying reset token");
            }
        }
    }

    public class TokenResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }
}