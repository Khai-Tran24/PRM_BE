using AutoMapper;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Chat;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Core.Configuration;

namespace BE_SaleHunter.Application.Services;

/// <summary>
/// Service for handling chat interactions with the AI assistant.
/// </summary>
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
    HttpClient httpClient,
    IOptions<OllamaConfiguration> ollamaOptions)
    : IChatService
{
    private readonly OllamaConfiguration _ollamaConfig = ollamaOptions.Value;

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
            var (aiResponseText, aiThinking) = await GetAiResponseAsync(request.Message, conversation.Messages.ToList(), user);

            // Add AI message
            var aiMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                Content = aiResponseText,
                AiThinking = aiThinking,
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

    private async Task<(string response, string thinking)> GetAiResponseAsync(string userMessage, List<ChatMessage> conversationHistory, User user)
    {
        try
        {
            // Build context with SaleHunter-specific information
            var contextBuilder = new StringBuilder();
            
            // Use configured system prompt template
            var systemPrompt = _ollamaConfig.SystemPromptTemplate
                .Replace("{userName}", user.Name ?? "User");

            // Add user's store information if they have one
            var storeInfo = "";
            if (user.Store != null)
            {
                storeInfo = $"The user owns a store called '{user.Store.Name}' located at {user.Store.Address}.";
            }
            systemPrompt = systemPrompt.Replace("{storeInfo}", storeInfo);

            contextBuilder.AppendLine(systemPrompt);

            // Build conversation history (limit to configured max messages)
            contextBuilder.AppendLine("\nConversation history:");
            var recentMessages = conversationHistory.TakeLast(_ollamaConfig.MaxContextMessages);
            
            foreach (var msg in recentMessages)
            {
                var sender = msg.IsUserMessage ? "User" : "Assistant";
                contextBuilder.AppendLine($"{sender}: {msg.Content}");
                
                // Include AI thinking in context for better continuity
                if (!msg.IsUserMessage && !string.IsNullOrEmpty(msg.AiThinking))
                {
                    contextBuilder.AppendLine($"[Previous thinking: {msg.AiThinking}]");
                }
            }

            contextBuilder.AppendLine($"\nUser: {userMessage}");
            contextBuilder.AppendLine("Assistant:");

            var ollamaRequest = new OllamaRequestDto
            {
                model = _ollamaConfig.DefaultModel,
                prompt = contextBuilder.ToString(),
                stream = _ollamaConfig.EnableStreaming
            };

            var requestJson = JsonSerializer.Serialize(ollamaRequest);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            // Set timeout from configuration
            httpClient.Timeout = TimeSpan.FromSeconds(_ollamaConfig.TimeoutSeconds);

            var response = await httpClient.PostAsync($"{_ollamaConfig.Endpoint}/api/generate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponseDto>(responseJson);
                var fullResponse = ollamaResponse?.response ?? "Sorry, I couldn't generate a response at the moment.";

                // Extract thinking if enabled
                if (_ollamaConfig.EnableThinkingExtraction)
                {
                    return ExtractThinkingFromResponse(fullResponse);
                }

                return (fullResponse, string.Empty);
            }

            logger.LogWarning("Ollama API returned error: {StatusCode}", response.StatusCode);
            return ("Sorry, I'm having trouble connecting to the AI service. Please try again later.", string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting AI response from Ollama");
            return ("Sorry, I encountered an error while processing your request. Please try again.", string.Empty);
        }
    }

    /// <summary>
    /// Extracts thinking content from AI response and returns cleaned response
    /// </summary>
    private (string response, string thinking) ExtractThinkingFromResponse(string fullResponse)
    {
        try
        {
            // Regex to match <think>...</think> tags (case insensitive, multiline)
            var thinkingPattern = @"<think\s*>(.*?)</think\s*>";
            var regex = new Regex(thinkingPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var thinking = new StringBuilder();
            var matches = regex.Matches(fullResponse);

            // Extract all thinking blocks
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var thinkingContent = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(thinkingContent))
                    {
                        if (thinking.Length > 0)
                            thinking.AppendLine("\n---\n");
                        thinking.Append(thinkingContent);
                    }
                }
            }

            // Remove thinking tags from the response
            var cleanedResponse = regex.Replace(fullResponse, "").Trim();

            // Clean up any extra whitespace
            cleanedResponse = Regex.Replace(cleanedResponse, @"\s+", " ").Trim();

            // If the response is empty after cleaning, return a fallback message
            if (string.IsNullOrEmpty(cleanedResponse))
            {
                cleanedResponse = "I understand your question and am ready to help.";
            }

            return (cleanedResponse, thinking.ToString());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error extracting thinking from AI response");
            // Return original response if extraction fails
            return (fullResponse, string.Empty);
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