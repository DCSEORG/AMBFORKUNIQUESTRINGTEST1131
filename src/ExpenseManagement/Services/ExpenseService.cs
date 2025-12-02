using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;
using System.Data;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetExpensesAsync(string? statusFilter = null, string? categoryFilter = null);
    Task<List<Expense>> GetPendingExpensesAsync();
    Task<Expense?> GetExpenseByIdAsync(int expenseId);
    Task<List<ExpenseCategory>> GetCategoriesAsync();
    Task<List<ExpenseStatus>> GetStatusesAsync();
    Task<List<User>> GetUsersAsync();
    Task<int> CreateExpenseAsync(CreateExpenseRequest request);
    Task<bool> SubmitExpenseAsync(int expenseId);
    Task<bool> ApproveExpenseAsync(int expenseId, int reviewerId);
    Task<bool> RejectExpenseAsync(int expenseId, int reviewerId);
}

public class ExpenseService : IExpenseService
{
    private readonly string _connectionString;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public async Task<List<Expense>> GetExpensesAsync(string? statusFilter = null, string? categoryFilter = null)
    {
        var expenses = new List<Expense>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC sp_GetExpenses @StatusFilter, @CategoryFilter", connection);
            command.Parameters.AddWithValue("@StatusFilter", (object?)statusFilter ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryFilter", (object?)categoryFilter ?? DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expenses from database");
            throw;
        }
        
        return expenses;
    }

    public async Task<List<Expense>> GetPendingExpensesAsync()
    {
        var expenses = new List<Expense>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC sp_GetPendingExpenses", connection);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending expenses from database");
            throw;
        }
        
        return expenses;
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC sp_GetExpenseById @ExpenseId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapExpense(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expense {ExpenseId} from database", expenseId);
            throw;
        }
        
        return null;
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        var categories = new List<ExpenseCategory>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC sp_GetCategories", connection);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching categories from database");
            throw;
        }
        
        return categories;
    }

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        var statuses = new List<ExpenseStatus>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC sp_GetStatuses", connection);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching statuses from database");
            throw;
        }
        
        return statuses;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        var users = new List<User>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC sp_GetUsers", connection);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
                    RoleName = reader.IsDBNull(reader.GetOrdinal("RoleName")) ? null : reader.GetString(reader.GetOrdinal("RoleName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from database");
            throw;
        }
        
        return users;
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC sp_CreateExpense @UserId, @CategoryId, @AmountMinor, @ExpenseDate, @Description", connection);
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense in database");
            throw;
        }
    }

    public async Task<bool> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC sp_SubmitExpense @ExpenseId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<bool> ApproveExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC sp_ApproveExpense @ExpenseId, @ReviewerId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
            throw;
        }
    }

    public async Task<bool> RejectExpenseAsync(int expenseId, int reviewerId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("EXEC sp_RejectExpense @ExpenseId, @ReviewerId", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
            throw;
        }
    }

    private static Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UserName = reader.IsDBNull(reader.GetOrdinal("UserName")) ? null : reader.GetString(reader.GetOrdinal("UserName")),
            CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusName = reader.IsDBNull(reader.GetOrdinal("StatusName")) ? null : reader.GetString(reader.GetOrdinal("StatusName")),
            ReviewerName = reader.IsDBNull(reader.GetOrdinal("ReviewerName")) ? null : reader.GetString(reader.GetOrdinal("ReviewerName"))
        };
    }
}
