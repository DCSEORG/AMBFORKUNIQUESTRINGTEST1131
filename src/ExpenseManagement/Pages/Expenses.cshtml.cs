using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ExpensesModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpensesModel> _logger;

    public List<Expense> Expenses { get; set; } = new();
    public List<ExpenseCategory> Categories { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }
    
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }

    public ExpensesModel(IExpenseService expenseService, ILogger<ExpensesModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Expenses = await _expenseService.GetExpensesAsync(StatusFilter, CategoryFilter);
            Categories = await _expenseService.GetCategoriesAsync();
            Statuses = await _expenseService.GetStatusesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expenses");
            ErrorMessage = "Failed to load expenses from database. Showing dummy data.";
            ErrorDetails = $"Error in ExpensesModel.cs at OnGetAsync: {ex.Message}";
            
            var dummyService = new DummyExpenseService(Microsoft.Extensions.Logging.Abstractions.NullLogger<DummyExpenseService>.Instance);
            Expenses = await dummyService.GetExpensesAsync(StatusFilter, CategoryFilter);
            Categories = await dummyService.GetCategoriesAsync();
            Statuses = await dummyService.GetStatusesAsync();
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(int expenseId)
    {
        try
        {
            await _expenseService.SubmitExpenseAsync(expenseId);
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", expenseId);
            ErrorMessage = $"Failed to submit expense: {ex.Message}";
            await OnGetAsync();
            return Page();
        }
    }
}
