using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(IExpenseService expenseService, ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all expenses with optional filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Expense>>>> GetExpenses(
        [FromQuery] string? status = null,
        [FromQuery] string? category = null)
    {
        try
        {
            var expenses = await _expenseService.GetExpensesAsync(status, category);
            return Ok(ApiResponse<List<Expense>>.Ok(expenses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses");
            return Ok(ApiResponse<List<Expense>>.Fail(
                "Failed to retrieve expenses",
                ex.Message,
                "ExpensesController.cs",
                30));
        }
    }

    /// <summary>
    /// Gets pending expenses awaiting approval
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<List<Expense>>>> GetPendingExpenses()
    {
        try
        {
            var expenses = await _expenseService.GetPendingExpensesAsync();
            return Ok(ApiResponse<List<Expense>>.Ok(expenses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending expenses");
            return Ok(ApiResponse<List<Expense>>.Fail(
                "Failed to retrieve pending expenses",
                ex.Message,
                "ExpensesController.cs",
                53));
        }
    }

    /// <summary>
    /// Gets a specific expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Expense>>> GetExpense(int id)
    {
        try
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                return NotFound(ApiResponse<Expense>.Fail($"Expense with ID {id} not found"));
            }
            return Ok(ApiResponse<Expense>.Ok(expense));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {Id}", id);
            return Ok(ApiResponse<Expense>.Fail(
                "Failed to retrieve expense",
                ex.Message,
                "ExpensesController.cs",
                77));
        }
    }

    /// <summary>
    /// Creates a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var expenseId = await _expenseService.CreateExpenseAsync(request);
            return Ok(ApiResponse<int>.Ok(expenseId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return Ok(ApiResponse<int>.Fail(
                "Failed to create expense",
                ex.Message,
                "ExpensesController.cs",
                97));
        }
    }

    /// <summary>
    /// Submits an expense for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<ApiResponse<bool>>> SubmitExpense(int id)
    {
        try
        {
            var success = await _expenseService.SubmitExpenseAsync(id);
            return Ok(ApiResponse<bool>.Ok(success));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {Id}", id);
            return Ok(ApiResponse<bool>.Fail(
                "Failed to submit expense",
                ex.Message,
                "ExpensesController.cs",
                117));
        }
    }

    /// <summary>
    /// Approves an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse<bool>>> ApproveExpense(int id, [FromBody] ApproveExpenseRequest request)
    {
        try
        {
            var success = await _expenseService.ApproveExpenseAsync(id, request.ReviewerId);
            return Ok(ApiResponse<bool>.Ok(success));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {Id}", id);
            return Ok(ApiResponse<bool>.Fail(
                "Failed to approve expense",
                ex.Message,
                "ExpensesController.cs",
                137));
        }
    }

    /// <summary>
    /// Rejects an expense
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ApiResponse<bool>>> RejectExpense(int id, [FromBody] RejectExpenseRequest request)
    {
        try
        {
            var success = await _expenseService.RejectExpenseAsync(id, request.ReviewerId);
            return Ok(ApiResponse<bool>.Ok(success));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {Id}", id);
            return Ok(ApiResponse<bool>.Fail(
                "Failed to reject expense",
                ex.Message,
                "ExpensesController.cs",
                157));
        }
    }

    /// <summary>
    /// Gets all expense categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<ExpenseCategory>>>> GetCategories()
    {
        try
        {
            var categories = await _expenseService.GetCategoriesAsync();
            return Ok(ApiResponse<List<ExpenseCategory>>.Ok(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return Ok(ApiResponse<List<ExpenseCategory>>.Fail(
                "Failed to retrieve categories",
                ex.Message,
                "ExpensesController.cs",
                177));
        }
    }

    /// <summary>
    /// Gets all expense statuses
    /// </summary>
    [HttpGet("statuses")]
    public async Task<ActionResult<ApiResponse<List<ExpenseStatus>>>> GetStatuses()
    {
        try
        {
            var statuses = await _expenseService.GetStatusesAsync();
            return Ok(ApiResponse<List<ExpenseStatus>>.Ok(statuses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses");
            return Ok(ApiResponse<List<ExpenseStatus>>.Fail(
                "Failed to retrieve statuses",
                ex.Message,
                "ExpensesController.cs",
                197));
        }
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<List<User>>>> GetUsers()
    {
        try
        {
            var users = await _expenseService.GetUsersAsync();
            return Ok(ApiResponse<List<User>>.Ok(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return Ok(ApiResponse<List<User>>.Fail(
                "Failed to retrieve users",
                ex.Message,
                "ExpensesController.cs",
                217));
        }
    }
}
