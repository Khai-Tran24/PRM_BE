using BE_SaleHunter.Core.Entities;

namespace BE_SaleHunter.Core.Interfaces;

public interface IChatConversationRepository : IGenericRepository<ChatConversation>
{
    Task<List<ChatConversation>> GetUserConversationsAsync(long userId);
    Task<ChatConversation?> GetConversationWithMessagesAsync(long conversationId, long userId);
}