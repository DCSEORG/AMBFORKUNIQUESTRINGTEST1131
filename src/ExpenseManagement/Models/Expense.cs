namespace ExpenseManagement.Models;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public string? RoleName { get; set; }
    public string? ManagerName { get; set; }
}

public class ExpenseCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ExpenseStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int StatusId { get; set; }
    public int AmountMinor { get; set; } // Amount in pence
    public string Currency { get; set; } = "GBP";
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public string? UserName { get; set; }
    public string? CategoryName { get; set; }
    public string? StatusName { get; set; }
    public string? ReviewerName { get; set; }
    
    // Computed property for display
    public decimal Amount => AmountMinor / 100m;
    public string AmountFormatted => $"£{Amount:N2}";
}

public class CreateExpenseRequest
{
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
}

public class ApproveExpenseRequest
{
    public int ExpenseId { get; set; }
    public int ReviewerId { get; set; }
}

public class RejectExpenseRequest
{
    public int ExpenseId { get; set; }
    public int ReviewerId { get; set; }
}
