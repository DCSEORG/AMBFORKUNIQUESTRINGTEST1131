using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ApproveModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ApproveModel> _logger;

    public List<Expense> PendingExpenses { get; set; } = new();
    public List<User> Managers { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int ReviewerId { get; set; } = 2; // Default to Bob Manager
    
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public string? SuccessMessage { get; set; }

    public ApproveModel(IExpenseService expenseService, ILogger<ApproveModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId, int reviewerId)
    {
        try
        {
            await _expenseService.ApproveExpenseAsync(expenseId, reviewerId);
            SuccessMessage = $"Expense #{expenseId} approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
            ErrorMessage = $"Failed to approve expense: {ex.Message}";
            ErrorDetails = $"Error in ApproveModel.cs at OnPostApproveAsync: {ex.Message}";
        }
        
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId, int reviewerId)
    {
        try
        {
            await _expenseService.RejectExpenseAsync(expenseId, reviewerId);
            SuccessMessage = $"Expense #{expenseId} rejected.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
            ErrorMessage = $"Failed to reject expense: {ex.Message}";
            ErrorDetails = $"Error in ApproveModel.cs at OnPostRejectAsync: {ex.Message}";
        }
        
        await LoadDataAsync();
        return Page();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            PendingExpenses = await _expenseService.GetPendingExpensesAsync();
            
            if (!string.IsNullOrEmpty(Filter))
            {
                PendingExpenses = PendingExpenses
                    .Where(e => 
                        (e.CategoryName?.Contains(Filter, StringComparison.OrdinalIgnoreCase) == true) ||
                        (e.Description?.Contains(Filter, StringComparison.OrdinalIgnoreCase) == true) ||
                        (e.UserName?.Contains(Filter, StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();
            }
            
            var users = await _expenseService.GetUsersAsync();
            Managers = users.Where(u => u.RoleName == "Manager").ToList();
            
            if (!Managers.Any())
            {
                Managers = users.Take(1).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data");
            ErrorMessage = "Failed to load data from database. Using dummy data.";
            ErrorDetails = $"Error in ApproveModel.cs at LoadDataAsync: {ex.Message}";
            
            var dummyService = new DummyExpenseService(Microsoft.Extensions.Logging.Abstractions.NullLogger<DummyExpenseService>.Instance);
            PendingExpenses = await dummyService.GetPendingExpensesAsync();
            var users = await dummyService.GetUsersAsync();
            Managers = users.Where(u => u.RoleName == "Manager").ToList();
        }
    }
}
