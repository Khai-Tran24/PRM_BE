using BE_SaleHunter.Core.Entities;

namespace BE_SaleHunter.Core.Interfaces;

public interface IChatMessageRepository : IGenericRepository<ChatMessage>
{
    Task<ChatMessage> AddMessageAsync(ChatMessage message);
    Task<List<ChatMessage>> GetConversationMessagesAsync(long conversationId);
}