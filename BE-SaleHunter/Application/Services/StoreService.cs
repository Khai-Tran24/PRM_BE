using AutoMapper;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Store;

namespace BE_SaleHunter.Application.Services
{
    public interface IStoreService
    {
        Task<BaseResponseDto<StoreDto>> CreateStoreAsync(CreateStoreDto createStoreDto, long userId);
        Task<BaseResponseDto<StoreDto>> GetStoreByIdAsync(long storeId);
        Task<BaseResponseDto<StoreDto>> GetStoreByUserIdAsync(long userId);
        Task<BaseResponseDto<StoreDto>> UpdateStoreAsync(long storeId, UpdateStoreDto updateStoreDto, long userId);
        Task<BaseResponseDto<bool>> DeleteStoreAsync(long storeId, long userId);
        Task<BaseResponseDto<IEnumerable<StoreDto>>> GetAllStoresAsync();

        Task<BaseResponseDto<IEnumerable<StoreDto>>> SearchStoresAsync(string query, decimal? latitude = null,
            decimal? longitude = null, double? radiusKm = null);

        Task<BaseResponseDto<IEnumerable<StoreDto>>> GetNearbyStoresAsync(decimal latitude, decimal longitude,
            double radiusKm = 10);
    }

    public class StoreService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<StoreService> logger,
        IImageStorageService imageStorageService,
        ILocationService locationService)
        : IStoreService
    {        public async Task<BaseResponseDto<StoreDto>> CreateStoreAsync(CreateStoreDto createStoreDto, long userId)
        {
            logger.LogInformation("SERVICE LAYER - CreateStoreAsync called for UserId: {UserId}, StoreName: {StoreName}", 
                userId, createStoreDto.Name);

            try
            {
                // Check if user already has a store
                logger.LogDebug("Checking if user {UserId} already has a store", userId);
                var existingStore = await unitOfWork.StoreRepository.GetByUserIdAsync(userId);
                if (existingStore != null)
                {
                    logger.LogWarning("User {UserId} already has a store with ID: {StoreId}", userId, existingStore.Id);
                    return BaseResponseDto<StoreDto>.Failure("User already has a store");
                }
                
                // Validate required fields
                if (string.IsNullOrWhiteSpace(createStoreDto.Name) ||
                    string.IsNullOrWhiteSpace(createStoreDto.Address))
                {
                    logger.LogWarning("Store creation failed - missing required fields for UserId: {UserId}", userId);
                    return BaseResponseDto<StoreDto>.Failure("Name and Address are required");
                }                logger.LogDebug("Geocoding address: {Address} for store creation", createStoreDto.Address);
                // Geocode the address
                var coordinates = await locationService.GeocodeAsync(createStoreDto.Address);
                
                logger.LogDebug("Geocoding result - Latitude: {Latitude}, Longitude: {Longitude}", 
                    coordinates?.Latitude, coordinates?.Longitude);

                var store = new Store
                {
                    Name = createStoreDto.Name,
                    Description = createStoreDto.Description,
                    Address = createStoreDto.Address,
                    Latitude = coordinates?.Latitude ?? 0,
                    Longitude = coordinates?.Longitude ?? 0,
                    Phone = createStoreDto.Phone,
                    Category = createStoreDto.Category,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                // Handle store image upload
                if (!string.IsNullOrWhiteSpace(createStoreDto.LogoBase64))
                {
                    logger.LogDebug("Uploading store logo for UserId: {UserId}", userId);
                    try
                    {
                        var imageUrl = await imageStorageService.UploadBase64ImageAsync(
                            createStoreDto.LogoBase64,
                            $"stores/{userId}/main");

                        store.LogoUrl = imageUrl;
                        logger.LogDebug("Store logo uploaded successfully: {ImageUrl}", imageUrl);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to upload store image for user {UserId}", userId);
                    }
                }

                logger.LogDebug("Adding store to repository for UserId: {UserId}", userId);
                await unitOfWork.StoreRepository.AddAsync(store);

                // Update user's StoreId
                logger.LogDebug("Updating user StoreId for UserId: {UserId}", userId);
                var user = await unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    user.StoreId = store.Id;
                    await unitOfWork.UserRepository.UpdateAsync(user);
                }

                await unitOfWork.CompleteAsync();
                logger.LogInformation("Store created successfully - StoreId: {StoreId}, UserId: {UserId}, StoreName: {StoreName}", 
                    store.Id, userId, store.Name);

                var storeDto = mapper.Map<StoreDto>(store);
                return BaseResponseDto<StoreDto>.Success(storeDto, "Store created successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating store for user: {UserId}", userId);
                return BaseResponseDto<StoreDto>.Failure("An error occurred while creating the store");
            }
        }        public async Task<BaseResponseDto<StoreDto>> GetStoreByIdAsync(long storeId)
        {
            logger.LogInformation("SERVICE LAYER - GetStoreByIdAsync called for StoreId: {StoreId}", storeId);

            try
            {
                logger.LogDebug("Fetching store with products from repository for StoreId: {StoreId}", storeId);
                var store = await unitOfWork.StoreRepository.GetStoreWithProductsAsync(storeId);
                if (store == null)
                {
                    logger.LogWarning("Store not found with ID: {StoreId}", storeId);
                    return BaseResponseDto<StoreDto>.Failure("Store not found");
                }

                logger.LogDebug("Store retrieved successfully - StoreId: {StoreId}, StoreName: {StoreName}", 
                    store.Id, store.Name);

                var storeDto = mapper.Map<StoreDto>(store);
                return BaseResponseDto<StoreDto>.Success(storeDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting store by ID: {StoreId}", storeId);
                return BaseResponseDto<StoreDto>.Failure("An error occurred while retrieving the store");
            }
        }

        public async Task<BaseResponseDto<StoreDto>> GetStoreByUserIdAsync(long userId)
        {
            try
            {
                var store = await unitOfWork.StoreRepository.GetByUserIdAsync(userId);
                if (store == null)
                {
                    return BaseResponseDto<StoreDto>.Failure("Store not found for this user");
                }

                var storeDto = mapper.Map<StoreDto>(store);
                return BaseResponseDto<StoreDto>.Success(storeDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting store by user ID: {UserId}", userId);
                return BaseResponseDto<StoreDto>.Failure("An error occurred while retrieving the store");
            }
        }

        public async Task<BaseResponseDto<StoreDto>> UpdateStoreAsync(long storeId, UpdateStoreDto updateStoreDto,
            long userId)
        {
            try
            {
                var store = await unitOfWork.StoreRepository.GetByIdAsync(storeId);
                if (store == null)
                {
                    return BaseResponseDto<StoreDto>.Failure("Store not found");
                }

                if (store.UserId != userId)
                {
                    return BaseResponseDto<StoreDto>.Failure("You are not authorized to update this store");
                }

                // Update store properties
                if (!string.IsNullOrWhiteSpace(updateStoreDto.Name))
                {
                    store.Name = updateStoreDto.Name;
                }

                if (!string.IsNullOrWhiteSpace(updateStoreDto.Description))
                {
                    store.Description = updateStoreDto.Description;
                }

                if (!string.IsNullOrWhiteSpace(updateStoreDto.Address))
                {
                    store.Address = updateStoreDto.Address;
                    // Update coordinates
                    var coordinates = await locationService.GeocodeAsync(updateStoreDto.Address);
                    if (coordinates != null)
                    {
                        store.Latitude = coordinates.Latitude;
                        store.Longitude = coordinates.Longitude;
                    }
                }

                if (!string.IsNullOrWhiteSpace(updateStoreDto.Phone))
                {
                    store.Phone = updateStoreDto.Phone;
                }

                if (!string.IsNullOrWhiteSpace(updateStoreDto.Category))
                {
                    store.Category = updateStoreDto.Category;
                }

                // Handle store image upload
                if (!string.IsNullOrWhiteSpace(updateStoreDto.LogoBase64))
                {
                    try
                    {
                        var imageUrl = await imageStorageService.UploadBase64ImageAsync(
                            updateStoreDto.LogoBase64,
                            $"stores/{userId}/main-{DateTime.UtcNow.Ticks}");

                        store.LogoUrl = imageUrl;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to upload updated store image for store {StoreId}", storeId);
                    }
                }

                store.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.StoreRepository.UpdateAsync(store);
                await unitOfWork.CompleteAsync();

                var storeDto = mapper.Map<StoreDto>(store);
                return BaseResponseDto<StoreDto>.Success(storeDto, "Store updated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating store: {StoreId}", storeId);
                return BaseResponseDto<StoreDto>.Failure("An error occurred while updating the store");
            }
        }

        public async Task<BaseResponseDto<bool>> DeleteStoreAsync(long storeId, long userId)
        {
            try
            {
                var store = await unitOfWork.StoreRepository.GetByIdAsync(storeId);
                if (store == null)
                {
                    return BaseResponseDto<bool>.Failure("Store not found");
                }

                if (store.UserId != userId)
                {
                    return BaseResponseDto<bool>.Failure("You are not authorized to delete this store");
                }

                // Hard delete for now (can be changed to soft delete if needed)
                await unitOfWork.StoreRepository.DeleteAsync(store);

                // Update user's StoreId
                var user = await unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    user.StoreId = null;
                    await unitOfWork.UserRepository.UpdateAsync(user);
                }

                await unitOfWork.CompleteAsync();

                return BaseResponseDto<bool>.Success(true, "Store deleted successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting store: {StoreId}", storeId);
                return BaseResponseDto<bool>.Failure("An error occurred while deleting the store");
            }
        }

        public async Task<BaseResponseDto<IEnumerable<StoreDto>>> GetAllStoresAsync()
        {
            try
            {
                var stores = await unitOfWork.StoreRepository.GetAllAsync();
                var storeDtos = mapper.Map<IEnumerable<StoreDto>>(stores);
                return BaseResponseDto<IEnumerable<StoreDto>>.Success(storeDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting all stores");
                return BaseResponseDto<IEnumerable<StoreDto>>.Failure("An error occurred while retrieving stores");
            }
        }

        public async Task<BaseResponseDto<IEnumerable<StoreDto>>> SearchStoresAsync(string query,
            decimal? latitude = null, decimal? longitude = null, double? radiusKm = null)
        {
            try
            {
                IEnumerable<Store> stores;
                if (latitude.HasValue && longitude.HasValue && radiusKm.HasValue)
                {
                    // Search by location first, then filter by query
                    var nearbyStores =
                        await unitOfWork.StoreRepository.GetStoresByLocationAsync(latitude.Value,
                            longitude.Value, radiusKm.Value);
                    stores = nearbyStores.Where(s =>
                        s.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        s.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        (s.Description != null && s.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));
                }
                else
                {
                    // Simple text search
                    stores = await unitOfWork.StoreRepository.SearchStoresAsync(query);
                }

                var storeDtos = mapper.Map<IEnumerable<StoreDto>>(stores);
                return BaseResponseDto<IEnumerable<StoreDto>>.Success(storeDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error searching stores with query: {Query}", query);
                return BaseResponseDto<IEnumerable<StoreDto>>.Failure("An error occurred while searching stores");
            }
        }

        public async Task<BaseResponseDto<IEnumerable<StoreDto>>> GetNearbyStoresAsync(decimal latitude,
            decimal longitude, double radiusKm = 10)
        {
            try
            {
                var stores =
                    await unitOfWork.StoreRepository.GetStoresByLocationAsync(latitude, longitude,
                        radiusKm);
                var storeDtos = mapper.Map<IEnumerable<StoreDto>>(stores);
                return BaseResponseDto<IEnumerable<StoreDto>>.Success(storeDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting nearby stores at coordinates: {Latitude}, {Longitude}", latitude,
                    longitude);
                return BaseResponseDto<IEnumerable<StoreDto>>.Failure(
                    "An error occurred while retrieving nearby stores");
            }
        }
    }
}