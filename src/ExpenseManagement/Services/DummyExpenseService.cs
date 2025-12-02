using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public class DummyExpenseService : IExpenseService
{
    private readonly ILogger<DummyExpenseService> _logger;

    public DummyExpenseService(ILogger<DummyExpenseService> logger)
    {
        _logger = logger;
    }

    public Task<List<Expense>> GetExpensesAsync(string? statusFilter = null, string? categoryFilter = null)
    {
        _logger.LogWarning("Using dummy data - database connection not available");
        var expenses = GetDummyExpenses();
        
        if (!string.IsNullOrEmpty(statusFilter))
        {
            expenses = expenses.Where(e => e.StatusName?.Equals(statusFilter, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }
        
        if (!string.IsNullOrEmpty(categoryFilter))
        {
            expenses = expenses.Where(e => e.CategoryName?.Contains(categoryFilter, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }
        
        return Task.FromResult(expenses);
    }

    public Task<List<Expense>> GetPendingExpensesAsync()
    {
        _logger.LogWarning("Using dummy data - database connection not available");
        return Task.FromResult(GetDummyExpenses().Where(e => e.StatusName == "Submitted").ToList());
    }

    public Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        _logger.LogWarning("Using dummy data - database connection not available");
        return Task.FromResult(GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId));
    }

    public Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        _logger.LogWarning("Using dummy data - database connection not available");
        return Task.FromResult(new List<ExpenseCategory>
        {
            new() { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new() { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new() { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new() { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new() { CategoryId = 5, CategoryName = "Other", IsActive = true }
        });
    }

    public Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        _logger.LogWarning("Using dummy data - database connection not available");
        return Task.FromResult(new List<ExpenseStatus>
        {
            new() { StatusId = 1, StatusName = "Draft" },
            new() { StatusId = 2, StatusName = "Submitted" },
            new() { StatusId = 3, StatusName = "Approved" },
            new() { StatusId = 4, StatusName = "Rejected" }
        });
    }

    public Task<List<User>> GetUsersAsync()
    {
        _logger.LogWarning("Using dummy data - database connection not available");
        return Task.FromResult(new List<User>
        {
            new() { UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee", IsActive = true },
            new() { UserId = 2, UserName = "Bob Manager", Email = "bob.manager@example.co.uk", RoleId = 2, RoleName = "Manager", IsActive = true }
        });
    }

    public Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        _logger.LogWarning("Using dummy data - expense not actually created");
        return Task.FromResult(999); // Dummy ID
    }

    public Task<bool> SubmitExpenseAsync(int expenseId)
    {
        _logger.LogWarning("Using dummy data - expense not actually submitted");
        return Task.FromResult(true);
    }

    public Task<bool> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        _logger.LogWarning("Using dummy data - expense not actually approved");
        return Task.FromResult(true);
    }

    public Task<bool> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        _logger.LogWarning("Using dummy data - expense not actually rejected");
        return Task.FromResult(true);
    }

    private static List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new()
            {
                ExpenseId = 1,
                UserId = 1,
                CategoryId = 1,
                StatusId = 2,
                AmountMinor = 12000,
                Currency = "GBP",
                ExpenseDate = new DateTime(2024, 1, 15),
                Description = "Taxi from airport to client site",
                UserName = "Alice Example",
                CategoryName = "Travel",
                StatusName = "Submitted",
                SubmittedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                ExpenseId = 2,
                UserId = 1,
                CategoryId = 2,
                StatusId = 2,
                AmountMinor = 6900,
                Currency = "GBP",
                ExpenseDate = new DateTime(2024, 1, 10),
                Description = "Client lunch meeting",
                UserName = "Alice Example",
                CategoryName = "Meals",
                StatusName = "Submitted",
                SubmittedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            },
            new()
            {
                ExpenseId = 3,
                UserId = 1,
                CategoryId = 3,
                StatusId = 3,
                AmountMinor = 9950,
                Currency = "GBP",
                ExpenseDate = new DateTime(2024, 12, 4),
                Description = "Office stationery",
                UserName = "Alice Example",
                CategoryName = "Supplies",
                StatusName = "Approved",
                SubmittedAt = DateTime.UtcNow.AddDays(-10),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.UtcNow.AddDays(-8),
                CreatedAt = DateTime.UtcNow.AddDays(-11)
            },
            new()
            {
                ExpenseId = 4,
                UserId = 1,
                CategoryId = 1,
                StatusId = 3,
                AmountMinor = 1920,
                Currency = "GBP",
                ExpenseDate = new DateTime(2024, 12, 18),
                Description = "Transport to conference",
                UserName = "Alice Example",
                CategoryName = "Travel",
                StatusName = "Approved",
                SubmittedAt = DateTime.UtcNow.AddDays(-15),
                ReviewedBy = 2,
                ReviewerName = "Bob Manager",
                ReviewedAt = DateTime.UtcNow.AddDays(-14),
                CreatedAt = DateTime.UtcNow.AddDays(-16)
            }
        };
    }
}
