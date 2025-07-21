using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_SaleHunter.Core.Entities
{
    public class ChatConversation : BaseEntity
    {
        [Required] [ForeignKey("User")] public long UserId { get; set; }

        [Required] [MaxLength(500)] public string Title { get; set; } = string.Empty;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}