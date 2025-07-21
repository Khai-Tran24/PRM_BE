using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_SaleHunter.Application.Services;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Chat;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BE_SaleHunter.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Send a message to the AI assistant
        /// </summary>
        [HttpPost("send")]
        public async Task<ActionResult<BaseResponseDto<SendMessageResponseDto>>> SendMessage([FromBody] SendMessageRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                {
                    return BadRequest(BaseResponseDto<SendMessageResponseDto>.Failure("Invalid user ID"));
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(BaseResponseDto<SendMessageResponseDto>.Failure("Message cannot be empty"));
                }

                if (request.Message.Length > 2000)
                {
                    return BadRequest(BaseResponseDto<SendMessageResponseDto>.Failure("Message is too long (max 2000 characters)"));
                }

                var result = await _chatService.SendMessageAsync(userId, request);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in send message endpoint");
                return StatusCode(500, BaseResponseDto<SendMessageResponseDto>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get all conversations for the current user
        /// </summary>
        [HttpGet("conversations")]
        public async Task<ActionResult<BaseResponseDto<List<ChatConversationDto>>>> GetConversations()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                {
                    return BadRequest(BaseResponseDto<List<ChatConversationDto>>.Failure("Invalid user ID"));
                }

                var result = await _chatService.GetUserConversationsAsync(userId);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in get conversations endpoint");
                return StatusCode(500, BaseResponseDto<List<ChatConversationDto>>.Failure("Internal server error"));
            }
        }

        /// <summary>
        /// Get a specific conversation with all messages
        /// </summary>
        [HttpGet("conversations/{conversationId}")]
        public async Task<ActionResult<BaseResponseDto<ChatConversationDto>>> GetConversation(long conversationId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                {
                    return BadRequest(BaseResponseDto<ChatConversationDto>.Failure("Invalid user ID"));
                }

                var result = await _chatService.GetConversationAsync(userId, conversationId);
                
                if (result.IsSuccess)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in get conversation endpoint");
                return StatusCode(500, BaseResponseDto<ChatConversationDto>.Failure("Internal server error"));
            }
        }
    }
}
