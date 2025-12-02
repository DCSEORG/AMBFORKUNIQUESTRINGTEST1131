using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    /// Check if chat service is configured
    /// </summary>
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new { configured = _chatService.IsConfigured });
    }

    /// <summary>
    /// Send a chat message
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> Chat([FromBody] ChatRequest request)
    {
        try
        {
            var response = await _chatService.GetChatResponseAsync(request.Message, request.History);
            return Ok(ApiResponse<string>.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return Ok(ApiResponse<string>.Fail(
                "Failed to process chat request",
                ex.Message,
                "ChatController.cs",
                42));
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessage>? History { get; set; }
}
