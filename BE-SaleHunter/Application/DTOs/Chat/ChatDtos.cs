namespace BE_SaleHunter.Application.DTOs.Chat
{
    public class ChatMessageDto
    {
        public long Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsUserMessage { get; set; }
        public string AiThinking { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ChatConversationDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();
    }

    public class SendMessageRequestDto
    {
        public long? ConversationId { get; set; } // null for new conversation
        public string Message { get; set; } = string.Empty;
    }

    public class SendMessageResponseDto
    {
        public long ConversationId { get; set; }
        public ChatMessageDto UserMessage { get; set; } = null!;
        public ChatMessageDto AiResponse { get; set; } = null!;
    }

    public class GetConversationsResponseDto
    {
        public List<ChatConversationDto> Conversations { get; set; } = new List<ChatConversationDto>();
    }

    // Ollama API DTOs
    public class OllamaRequestDto
    {
        public string model { get; set; } = "llama3.2:1b";
        public string prompt { get; set; } = string.Empty;
        public bool stream { get; set; } = false;
    }

    public class OllamaResponseDto
    {
        public string response { get; set; } = string.Empty;
        public bool done { get; set; }
    }
}
