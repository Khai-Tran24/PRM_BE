using AutoMapper;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Application.DTOs;

namespace BE_SaleHunter.Application.Services
{
    public interface IUserService
    {
        Task<BaseResponseDto<UserDto>> GetUserByIdAsync(long userId);
        Task<BaseResponseDto<UserDto>> UpdateUserAsync(long userId, UpdateUserDto updateUserDto);
        Task<BaseResponseDto<IEnumerable<UserDto>>> GetAllUsersAsync();
        Task<BaseResponseDto<bool>> DeactivateUserAsync(long userId);
        Task<BaseResponseDto<bool>> ActivateUserAsync(long userId);
        Task<BaseResponseDto<UserDto>> GetUserProfileAsync(long userId);
    }

    public class UserService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UserService> logger,
        IImageStorageService imageStorageService)
        : IUserService
    {
        public async Task<BaseResponseDto<UserDto>> GetUserByIdAsync(long userId)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return BaseResponseDto<UserDto>.Failure("User not found");
                }

                var userDto = mapper.Map<UserDto>(user);
                return BaseResponseDto<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return BaseResponseDto<UserDto>.Failure("An error occurred while retrieving user");
            }
        }

        public async Task<BaseResponseDto<UserDto>> UpdateUserAsync(long userId, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return BaseResponseDto<UserDto>.Failure("User not found");
                }

                // Update user properties
                if (!string.IsNullOrWhiteSpace(updateUserDto.Name))
                {
                    user.Name = updateUserDto.Name;
                }

                if (!string.IsNullOrWhiteSpace(updateUserDto.PhoneNumber))
                {
                    user.PhoneNumber = updateUserDto.PhoneNumber;
                }

                // Handle profile image upload
                if (!string.IsNullOrWhiteSpace(updateUserDto.ProfileImageBase64))
                {
                    try
                    {
                        var imageUrl = await imageStorageService.UploadBase64ImageAsync(
                            updateUserDto.ProfileImageBase64,
                            $"users/{userId}/profile");

                        user.ProfileImageUrl = imageUrl;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to upload profile image for user {UserId}", userId);
                        // Continue without failing the entire update
                    }
                }

                user.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.UserRepository.UpdateAsync(user);
                await unitOfWork.CompleteAsync();

                var userDto = mapper.Map<UserDto>(user);
                return BaseResponseDto<UserDto>.Success(userDto, "Profile updated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating user: {UserId}", userId);
                return BaseResponseDto<UserDto>.Failure("An error occurred while updating user profile");
            }
        }

        public async Task<BaseResponseDto<IEnumerable<UserDto>>> GetAllUsersAsync()
        {
            try
            {
                var users = await unitOfWork.UserRepository.GetAllAsync();
                var userDtos = mapper.Map<IEnumerable<UserDto>>(users);
                return BaseResponseDto<IEnumerable<UserDto>>.Success(userDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting all users");
                return BaseResponseDto<IEnumerable<UserDto>>.Failure("An error occurred while retrieving users");
            }
        }

        public async Task<BaseResponseDto<bool>> DeactivateUserAsync(long userId)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return BaseResponseDto<bool>.Failure("User not found");
                }

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.UserRepository.UpdateAsync(user);
                await unitOfWork.CompleteAsync();

                return BaseResponseDto<bool>.Success(true, "User deactivated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deactivating user: {UserId}", userId);
                return BaseResponseDto<bool>.Failure("An error occurred while deactivating user");
            }
        }

        public async Task<BaseResponseDto<bool>> ActivateUserAsync(long userId)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return BaseResponseDto<bool>.Failure("User not found");
                }

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.UserRepository.UpdateAsync(user);
                await unitOfWork.CompleteAsync();

                return BaseResponseDto<bool>.Success(true, "User activated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error activating user: {UserId}", userId);
                return BaseResponseDto<bool>.Failure("An error occurred while activating user");
            }
        }

        public async Task<BaseResponseDto<UserDto>> GetUserProfileAsync(long userId)
        {
            try
            {
                var user = await unitOfWork.UserRepository.GetByIdWithStoreAsync(userId);
                if (user == null)
                {
                    return BaseResponseDto<UserDto>.Failure("User not found");
                }

                var userDto = mapper.Map<UserDto>(user);
                return BaseResponseDto<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting user profile: {UserId}", userId);
                return BaseResponseDto<UserDto>.Failure("An error occurred while retrieving user profile");
            }
        }
    }
}