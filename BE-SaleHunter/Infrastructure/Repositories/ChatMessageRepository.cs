using Microsoft.EntityFrameworkCore;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;

namespace BE_SaleHunter.Infrastructure.Repositories;

public class ChatMessageRepository(SaleHunterDbContext context, ILogger<GenericRepository<ChatMessage>> logger)
    : GenericRepository<ChatMessage>(context, logger), IChatMessageRepository
{
    public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
    {
        Context.ChatMessages.Add(message);
        await Context.SaveChangesAsync();
        return message;
    }

    public async Task<List<ChatMessage>> GetConversationMessagesAsync(long conversationId)
    {
        return await Context.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }
}