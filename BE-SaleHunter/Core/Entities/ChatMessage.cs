using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_SaleHunter.Core.Entities
{
    public class ChatMessage : BaseEntity
    {
        [Required]
        [ForeignKey("ChatConversation")]
        public long ConversationId { get; set; }

        [Required] [DataType("text")] public string Content { get; set; } = string.Empty;

        [Required] public bool IsUserMessage { get; set; } // true for user, false for AI

        // Navigation properties
        public virtual ChatConversation ChatConversation { get; set; } = null!;
    }
}