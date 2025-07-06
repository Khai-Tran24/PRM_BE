namespace BE_SaleHunter.Application.DTOs
{    public class UserDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int SignedInWith { get; set; }
        public long? StoreId { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public bool HasStore { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }public class UpdateUserDto
    {
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfileImageBase64 { get; set; }
    }

    public class UserResponseDto : BaseResponseDto
    {
        public UserDto? User { get; set; }
    }
}
