using AutoMapper;
using System.Text;
using System.Text.Json;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Chat;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;

namespace BE_SaleHunter.Application.Services;

public interface IChatService
{
    Task<BaseResponseDto<SendMessageResponseDto>> SendMessageAsync(long userId, SendMessageRequestDto request);
    Task<BaseResponseDto<List<ChatConversationDto>>> GetUserConversationsAsync(long userId);
    Task<BaseResponseDto<ChatConversationDto>> GetConversationAsync(long userId, long conversationId);
}

public class ChatService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<ChatService> logger,
    HttpClient httpClient)
    : IChatService
{
    private readonly string _ollamaEndpoint = "http://localhost:11434";

    public async Task<BaseResponseDto<SendMessageResponseDto>> SendMessageAsync(long userId,
        SendMessageRequestDto request)
    {
        try
        {
            // Validate user exists
            var user = await unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return BaseResponseDto<SendMessageResponseDto>.Failure("User not found",
                    ResponseCodes.FailedNotFound);
            }

            ChatConversation? conversation;

            // Get or create conversation
            if (request.ConversationId.HasValue)
            {
                conversation =
                    await unitOfWork.ChatConversationRepository.GetConversationWithMessagesAsync(
                        request.ConversationId.Value, userId);
                if (conversation == null)
                {
                    return BaseResponseDto<SendMessageResponseDto>.Failure("Conversation not found",
                        ResponseCodes.FailedNotFound);
                }
            }
            else
            {
                // Create new conversation
                conversation = new ChatConversation
                {
                    UserId = userId,
                    Title = GenerateConversationTitle(request.Message)
                };
                conversation = await unitOfWork.ChatConversationRepository.AddAsync(conversation);
                
                // Save the conversation first to get the ID
                await unitOfWork.CompleteAsync();
            }

            // Add user message
            var userMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                Content = request.Message,
                IsUserMessage = true
            };
            userMessage = await unitOfWork.ChatMessageRepository.AddMessageAsync(userMessage);

            // Get AI response with SaleHunter context
            var aiResponseText = await GetAiResponseAsync(request.Message, conversation.Messages.ToList(), user);

            // Add AI message
            var aiMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                Content = aiResponseText,
                IsUserMessage = false
            };
            aiMessage = await unitOfWork.ChatMessageRepository.AddMessageAsync(aiMessage);

            // Update conversation timestamp
            conversation.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.ChatConversationRepository.UpdateAsync(conversation);
            
            // Save all changes
            await unitOfWork.CompleteAsync();

            var response = new SendMessageResponseDto
            {
                ConversationId = conversation.Id,
                UserMessage = mapper.Map<ChatMessageDto>(userMessage),
                AiResponse = mapper.Map<ChatMessageDto>(aiMessage)
            };

            return BaseResponseDto<SendMessageResponseDto>.Success(response, "Message sent successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending chat message");
            return BaseResponseDto<SendMessageResponseDto>.Failure("Failed to send message",
                ResponseCodes.FailedServerError);
        }
    }

    public async Task<BaseResponseDto<List<ChatConversationDto>>> GetUserConversationsAsync(long userId)
    {
        try
        {
            var conversations = await unitOfWork.ChatConversationRepository.GetUserConversationsAsync(userId);
            var conversationDtos = mapper.Map<List<ChatConversationDto>>(conversations);

            return BaseResponseDto<List<ChatConversationDto>>.Success(conversationDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user conversations");
            return BaseResponseDto<List<ChatConversationDto>>.Failure("Failed to get conversations",
                ResponseCodes.FailedServerError);
        }
    }

    public async Task<BaseResponseDto<ChatConversationDto>> GetConversationAsync(long userId, long conversationId)
    {
        try
        {
            var conversation =
                await unitOfWork.ChatConversationRepository.GetConversationWithMessagesAsync(conversationId,
                    userId);
            if (conversation == null)
            {
                return BaseResponseDto<ChatConversationDto>.Failure("Conversation not found",
                    ResponseCodes.FailedNotFound);
            }

            var conversationDto = mapper.Map<ChatConversationDto>(conversation);
            return BaseResponseDto<ChatConversationDto>.Success(conversationDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting conversation");
            return BaseResponseDto<ChatConversationDto>.Failure("Failed to get conversation",
                ResponseCodes.FailedServerError);
        }
    }

    private async Task<string> GetAiResponseAsync(string userMessage, List<ChatMessage> conversationHistory,
        User user)
    {
        try
        {
            // Build context with SaleHunter-specific information
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine(
                "You are an AI assistant for SaleHunter, a price comparison mobile application.");
            contextBuilder.AppendLine(
                "SaleHunter helps users find the best prices for products across different stores.");
            contextBuilder.AppendLine($"The user's name is {user.Name}.");

            // Add user's store information if they have one
            if (user.Store != null)
            {
                contextBuilder.AppendLine(
                    $"The user owns a store called '{user.Store.Name}' located at {user.Store.Address}.");
            }

            // Add recent popular products context (simplified for now)
            contextBuilder.AppendLine("\nYou should help users with:");
            contextBuilder.AppendLine("- Finding products and comparing prices");
            contextBuilder.AppendLine("- Information about stores and their locations");
            contextBuilder.AppendLine("- General questions about using the SaleHunter app");
            contextBuilder.AppendLine("- Product recommendations and shopping advice");

            // Build conversation history
            contextBuilder.AppendLine("\nConversation history:");
            foreach (var msg in conversationHistory.TakeLast(10)) // Limit to recent messages
            {
                var sender = msg.IsUserMessage ? "User" : "Assistant";
                contextBuilder.AppendLine($"{sender}: {msg.Content}");
            }

            contextBuilder.AppendLine($"\nUser: {userMessage}");
            contextBuilder.AppendLine("Assistant:");

            var ollamaRequest = new OllamaRequestDto
            {
                model = "llama3.2:1b",
                prompt = contextBuilder.ToString(),
                stream = false
            };

            var requestJson = JsonSerializer.Serialize(ollamaRequest);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{_ollamaEndpoint}/api/generate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponseDto>(responseJson);
                return ollamaResponse?.response ?? "Sorry, I couldn't generate a response at the moment.";
            }

            logger.LogWarning("Ollama API returned error: {StatusCode}", response.StatusCode);
            return "Sorry, I'm having trouble connecting to the AI service. Please try again later.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting AI response from Ollama");
            return "Sorry, I encountered an error while processing your request. Please try again.";
        }
    }

    private string GenerateConversationTitle(string firstMessage)
    {
        // Simple title generation - take first 50 characters
        if (firstMessage.Length <= 50)
            return firstMessage;

        return firstMessage.Substring(0, 47) + "...";
    }
}