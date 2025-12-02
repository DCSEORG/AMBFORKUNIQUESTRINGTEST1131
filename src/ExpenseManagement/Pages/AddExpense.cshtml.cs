using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<AddExpenseModel> _logger;

    public List<ExpenseCategory> Categories { get; set; } = new();
    public List<User> Users { get; set; } = new();
    
    [BindProperty]
    public decimal Amount { get; set; }
    
    [BindProperty]
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    
    [BindProperty]
    public int CategoryId { get; set; }
    
    [BindProperty]
    public string? Description { get; set; }
    
    [BindProperty]
    public int UserId { get; set; }
    
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public string? SuccessMessage { get; set; }

    public AddExpenseModel(IExpenseService expenseService, ILogger<AddExpenseModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        await LoadFormDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var request = new CreateExpenseRequest
            {
                UserId = UserId,
                CategoryId = CategoryId,
                Amount = Amount,
                ExpenseDate = ExpenseDate,
                Description = Description
            };

            var expenseId = await _expenseService.CreateExpenseAsync(request);
            SuccessMessage = $"Expense created successfully with ID: {expenseId}";
            
            // Clear form
            Amount = 0;
            Description = null;
            ExpenseDate = DateTime.Today;
            
            await LoadFormDataAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ErrorMessage = "Failed to create expense";
            ErrorDetails = $"Error in AddExpenseModel.cs at OnPostAsync: {ex.Message}";
            await LoadFormDataAsync();
            return Page();
        }
    }

    private async Task LoadFormDataAsync()
    {
        try
        {
            Categories = await _expenseService.GetCategoriesAsync();
            Users = await _expenseService.GetUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading form data");
            ErrorMessage = "Failed to load form data from database. Using dummy data.";
            ErrorDetails = $"Error in AddExpenseModel.cs at LoadFormDataAsync: {ex.Message}";
            
            var dummyService = new DummyExpenseService(Microsoft.Extensions.Logging.Abstractions.NullLogger<DummyExpenseService>.Instance);
            Categories = await dummyService.GetCategoriesAsync();
            Users = await dummyService.GetUsersAsync();
        }
    }
}
