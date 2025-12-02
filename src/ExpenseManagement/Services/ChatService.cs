using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using System.Text.Json;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<string> GetChatResponseAsync(string userMessage, List<ChatMessage>? history = null);
    bool IsConfigured { get; }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly IExpenseService _expenseService;
    private OpenAIClient? _client;
    private readonly string? _deploymentName;
    private readonly string? _endpoint;

    public bool IsConfigured => !string.IsNullOrEmpty(_endpoint) && !string.IsNullOrEmpty(_deploymentName);

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, IExpenseService expenseService)
    {
        _configuration = configuration;
        _logger = logger;
        _expenseService = expenseService;
        _endpoint = _configuration["OpenAI:Endpoint"];
        _deploymentName = _configuration["OpenAI:DeploymentName"] ?? "gpt-4o";

        if (IsConfigured)
        {
            try
            {
                var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
                Azure.Core.TokenCredential credential;

                if (!string.IsNullOrEmpty(managedIdentityClientId))
                {
                    _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                    credential = new ManagedIdentityCredential(managedIdentityClientId);
                }
                else
                {
                    _logger.LogInformation("Using DefaultAzureCredential");
                    credential = new DefaultAzureCredential();
                }

                _client = new OpenAIClient(new Uri(_endpoint!), credential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OpenAI client");
            }
        }
    }

    public async Task<string> GetChatResponseAsync(string userMessage, List<ChatMessage>? history = null)
    {
        if (!IsConfigured || _client == null)
        {
            return "GenAI services are not configured. Please run deploy-with-chat.sh to deploy Azure OpenAI and enable the chat experience.";
        }

        try
        {
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(GetSystemPrompt())
                }
            };

            // Add history
            if (history != null)
            {
                foreach (var msg in history)
                {
                    if (msg.Role == "user")
                        chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(msg.Content));
                    else if (msg.Role == "assistant")
                        chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(msg.Content));
                }
            }

            // Add current user message
            chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(userMessage));

            // Define available functions
            var functions = GetFunctionDefinitions();
            foreach (var function in functions)
            {
                chatCompletionsOptions.Tools.Add(function);
            }

            var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
            var responseChoice = response.Value.Choices[0];

            // Handle function calls
            while (responseChoice.FinishReason == CompletionsFinishReason.ToolCalls)
            {
                chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(responseChoice.Message));

                foreach (var toolCall in responseChoice.Message.ToolCalls)
                {
                    if (toolCall is ChatCompletionsFunctionToolCall functionToolCall)
                    {
                        var functionResult = await ExecuteFunctionAsync(functionToolCall.Name, functionToolCall.Arguments);
                        chatCompletionsOptions.Messages.Add(new ChatRequestToolMessage(functionResult, functionToolCall.Id));
                    }
                }

                response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
                responseChoice = response.Value.Choices[0];
            }

            return responseChoice.Message.Content ?? "I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat response");
            return $"Error communicating with AI service: {ex.Message}";
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are an AI assistant for the Expense Management System. You help users manage their expenses.

You have access to the following functions to interact with the expense database:
- get_expenses: Retrieves all expenses, optionally filtered by status or category
- get_pending_expenses: Retrieves expenses awaiting approval
- get_categories: Retrieves available expense categories
- create_expense: Creates a new expense entry
- approve_expense: Approves a submitted expense
- reject_expense: Rejects a submitted expense

When listing expenses or data, format them nicely:
- Use numbered lists for multiple items
- Show amounts in £ format
- Include relevant details like date, category, and status

Be helpful, concise, and proactive in suggesting actions the user might want to take.";
    }

    private List<ChatCompletionsToolDefinition> GetFunctionDefinitions()
    {
        return new List<ChatCompletionsToolDefinition>
        {
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_expenses",
                Description = "Retrieves expenses from the database, optionally filtered by status or category",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        statusFilter = new { type = "string", description = "Filter by status: Draft, Submitted, Approved, Rejected" },
                        categoryFilter = new { type = "string", description = "Filter by category name" }
                    }
                })
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_pending_expenses",
                Description = "Retrieves all expenses with 'Submitted' status that are pending approval",
                Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_categories",
                Description = "Retrieves all available expense categories",
                Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "create_expense",
                Description = "Creates a new expense entry",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        userId = new { type = "integer", description = "The ID of the user creating the expense" },
                        categoryId = new { type = "integer", description = "The ID of the expense category" },
                        amount = new { type = "number", description = "The expense amount in pounds (e.g., 25.50)" },
                        expenseDate = new { type = "string", description = "The date of the expense in YYYY-MM-DD format" },
                        description = new { type = "string", description = "Description of the expense" }
                    },
                    required = new[] { "userId", "categoryId", "amount", "expenseDate" }
                })
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "approve_expense",
                Description = "Approves a submitted expense",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        expenseId = new { type = "integer", description = "The ID of the expense to approve" },
                        reviewerId = new { type = "integer", description = "The ID of the manager approving the expense" }
                    },
                    required = new[] { "expenseId", "reviewerId" }
                })
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "reject_expense",
                Description = "Rejects a submitted expense",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        expenseId = new { type = "integer", description = "The ID of the expense to reject" },
                        reviewerId = new { type = "integer", description = "The ID of the manager rejecting the expense" }
                    },
                    required = new[] { "expenseId", "reviewerId" }
                })
            }
        };
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
    {
        try
        {
            var args = JsonDocument.Parse(arguments);

            switch (functionName)
            {
                case "get_expenses":
                    var statusFilter = args.RootElement.TryGetProperty("statusFilter", out var sf) ? sf.GetString() : null;
                    var categoryFilter = args.RootElement.TryGetProperty("categoryFilter", out var cf) ? cf.GetString() : null;
                    var expenses = await _expenseService.GetExpensesAsync(statusFilter, categoryFilter);
                    return JsonSerializer.Serialize(expenses.Select(e => new
                    {
                        e.ExpenseId,
                        e.Description,
                        e.AmountFormatted,
                        e.ExpenseDate,
                        e.CategoryName,
                        e.StatusName,
                        e.UserName
                    }));

                case "get_pending_expenses":
                    var pending = await _expenseService.GetPendingExpensesAsync();
                    return JsonSerializer.Serialize(pending.Select(e => new
                    {
                        e.ExpenseId,
                        e.Description,
                        e.AmountFormatted,
                        e.ExpenseDate,
                        e.CategoryName,
                        e.UserName
                    }));

                case "get_categories":
                    var categories = await _expenseService.GetCategoriesAsync();
                    return JsonSerializer.Serialize(categories);

                case "create_expense":
                    var userId = args.RootElement.GetProperty("userId").GetInt32();
                    var categoryId = args.RootElement.GetProperty("categoryId").GetInt32();
                    var amount = args.RootElement.GetProperty("amount").GetDecimal();
                    var expenseDateStr = args.RootElement.GetProperty("expenseDate").GetString()!;
                    if (!DateTime.TryParse(expenseDateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var expenseDate))
                    {
                        expenseDate = DateTime.UtcNow.Date;
                    }
                    var description = args.RootElement.TryGetProperty("description", out var desc) ? desc.GetString() : null;
                    var newId = await _expenseService.CreateExpenseAsync(new CreateExpenseRequest
                    {
                        UserId = userId,
                        CategoryId = categoryId,
                        Amount = amount,
                        ExpenseDate = expenseDate,
                        Description = description
                    });
                    return JsonSerializer.Serialize(new { success = true, expenseId = newId });

                case "approve_expense":
                    var approveExpenseId = args.RootElement.GetProperty("expenseId").GetInt32();
                    var approveReviewerId = args.RootElement.GetProperty("reviewerId").GetInt32();
                    var approved = await _expenseService.ApproveExpenseAsync(approveExpenseId, approveReviewerId);
                    return JsonSerializer.Serialize(new { success = approved });

                case "reject_expense":
                    var rejectExpenseId = args.RootElement.GetProperty("expenseId").GetInt32();
                    var rejectReviewerId = args.RootElement.GetProperty("reviewerId").GetInt32();
                    var rejected = await _expenseService.RejectExpenseAsync(rejectExpenseId, rejectReviewerId);
                    return JsonSerializer.Serialize(new { success = rejected });

                default:
                    return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}

public class DummyChatService : IChatService
{
    public bool IsConfigured => false;

    public Task<string> GetChatResponseAsync(string userMessage, List<ChatMessage>? history = null)
    {
        return Task.FromResult("GenAI services are not configured. Please run deploy-with-chat.sh to deploy Azure OpenAI and AI Search resources to enable the chat experience with natural language database interaction.");
    }
}
