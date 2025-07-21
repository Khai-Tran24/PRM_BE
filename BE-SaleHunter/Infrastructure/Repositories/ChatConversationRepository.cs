using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BE_SaleHunter.Infrastructure.Repositories;

public class ChatConversationRepository(
    SaleHunterDbContext context,
    ILogger<GenericRepository<ChatConversation>> logger)
    : GenericRepository<ChatConversation>(context, logger), IChatConversationRepository
{
    public async Task<List<ChatConversation>> GetUserConversationsAsync(long userId)
    {
        return await Context.ChatConversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Include(c => c.Messages)
            .ToListAsync();
    }

    public async Task<ChatConversation?> GetConversationWithMessagesAsync(
        long conversationId, long userId)
    {
        return await Context.ChatConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);
    }
}