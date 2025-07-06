using System.ComponentModel.DataAnnotations;

namespace BE_SaleHunter.Core.Entities
{
    public class User : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? ProfileImageUrl { get; set; }
        
        public DateTime? LastLoginDate { get; set; }
        
        public int SignedInWith { get; set; } // 0=Email, 1=Google, 2=Facebook
        
        public bool IsActive { get; set; } = true;

        // Refresh token fields
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        // Password reset fields
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }        
        public long? StoreId { get; set; }

        // Navigation properties
        public virtual Store? Store { get; set; }
        public virtual ICollection<ProductRating> ProductRatings { get; set; } = new List<ProductRating>();
        public virtual ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
        public virtual ICollection<ProductView> ProductViews { get; set; } = new List<ProductView>();

        // Helper methods
        public bool HasStore() => StoreId.HasValue && StoreId.Value > 0;
        
        public string GetAccountType() => HasStore() ? "Seller Account" : "User Account";
    }
}
