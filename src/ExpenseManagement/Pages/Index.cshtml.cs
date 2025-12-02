using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;
using System.Text.RegularExpressions;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<IndexModel> _logger;
    private static readonly Regex SafeCssClassRegex = new(@"^[a-zA-Z0-9-_]+$", RegexOptions.Compiled);

    public List<Expense> RecentExpenses { get; set; } = new();
    public int TotalExpenses { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string TotalAmountFormatted => $"£{TotalAmount:N2}";
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }

    public IndexModel(IExpenseService expenseService, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public static string GetSafeStatusCssClass(string? statusName)
    {
        if (string.IsNullOrEmpty(statusName))
            return "unknown";
        
        var sanitized = statusName.ToLowerInvariant();
        return SafeCssClassRegex.IsMatch(sanitized) ? sanitized : "unknown";
    }

    public async Task OnGetAsync()
    {
        try
        {
            var expenses = await _expenseService.GetExpensesAsync();
            RecentExpenses = expenses.OrderByDescending(e => e.ExpenseDate).Take(5).ToList();
            TotalExpenses = expenses.Count;
            PendingCount = expenses.Count(e => e.StatusName == "Submitted");
            ApprovedCount = expenses.Count(e => e.StatusName == "Approved");
            TotalAmount = expenses.Sum(e => e.Amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            ErrorMessage = "Failed to connect to database. Showing dummy data.";
            ErrorDetails = GetManagedIdentityHelpText(ex);
            
            // Load dummy data
            var dummyService = new DummyExpenseService(_logger as ILogger<DummyExpenseService> 
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DummyExpenseService>.Instance);
            var expenses = await dummyService.GetExpensesAsync();
            RecentExpenses = expenses.OrderByDescending(e => e.ExpenseDate).Take(5).ToList();
            TotalExpenses = expenses.Count;
            PendingCount = expenses.Count(e => e.StatusName == "Submitted");
            ApprovedCount = expenses.Count(e => e.StatusName == "Approved");
            TotalAmount = expenses.Sum(e => e.Amount);
        }
    }

    private string GetManagedIdentityHelpText(Exception ex)
    {
        if (ex.Message.Contains("Managed Identity") || ex.Message.Contains("managed identity"))
        {
            return "Managed Identity authentication failed. Ensure: 1) The App Service has a user-assigned managed identity attached, 2) The managed identity has been granted access to the SQL database using sp_addrolemember, 3) The connection string uses 'Authentication=Active Directory Managed Identity' and includes the correct User Id (client ID of the managed identity).";
        }
        
        if (ex.Message.Contains("Login failed") || ex.Message.Contains("login failed"))
        {
            return "Database login failed. Check if the managed identity has been added as a user in the database and has the required permissions (db_datareader, db_datawriter).";
        }
        
        return $"Error in IndexModel.cs at OnGetAsync: {ex.Message}";
    }
}
