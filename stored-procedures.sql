-- Stored Procedures for Expense Management System
-- These stored procedures handle all data access operations

SET NOCOUNT ON;
GO

-- Get all expenses with optional filtering
CREATE OR ALTER PROCEDURE sp_GetExpenses
    @StatusFilter NVARCHAR(50) = NULL,
    @CategoryFilter NVARCHAR(100) = NULL
AS
BEGIN
    SELECT 
        e.ExpenseId,
        e.UserId,
        e.CategoryId,
        e.StatusId,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        e.ReviewedAt,
        e.CreatedAt,
        u.UserName,
        c.CategoryName,
        s.StatusName,
        r.UserName AS ReviewerName
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
    WHERE (@StatusFilter IS NULL OR s.StatusName = @StatusFilter)
      AND (@CategoryFilter IS NULL OR c.CategoryName LIKE '%' + @CategoryFilter + '%')
    ORDER BY e.ExpenseDate DESC;
END;
GO

-- Get pending expenses (Submitted status)
CREATE OR ALTER PROCEDURE sp_GetPendingExpenses
AS
BEGIN
    SELECT 
        e.ExpenseId,
        e.UserId,
        e.CategoryId,
        e.StatusId,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        e.ReviewedAt,
        e.CreatedAt,
        u.UserName,
        c.CategoryName,
        s.StatusName,
        NULL AS ReviewerName
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE s.StatusName = 'Submitted'
    ORDER BY e.SubmittedAt ASC;
END;
GO

-- Get expense by ID
CREATE OR ALTER PROCEDURE sp_GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SELECT 
        e.ExpenseId,
        e.UserId,
        e.CategoryId,
        e.StatusId,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        e.ReviewedAt,
        e.CreatedAt,
        u.UserName,
        c.CategoryName,
        s.StatusName,
        r.UserName AS ReviewerName
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
    WHERE e.ExpenseId = @ExpenseId;
END;
GO

-- Get all categories
CREATE OR ALTER PROCEDURE sp_GetCategories
AS
BEGIN
    SELECT CategoryId, CategoryName, IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END;
GO

-- Get all statuses
CREATE OR ALTER PROCEDURE sp_GetStatuses
AS
BEGIN
    SELECT StatusId, StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END;
GO

-- Get all users
CREATE OR ALTER PROCEDURE sp_GetUsers
AS
BEGIN
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.IsActive
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    WHERE u.IsActive = 1
    ORDER BY u.UserName;
END;
GO

-- Create new expense
CREATE OR ALTER PROCEDURE sp_CreateExpense
    @UserId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL
AS
BEGIN
    DECLARE @StatusId INT;
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft';
    
    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, CreatedAt)
    VALUES (@UserId, @CategoryId, @StatusId, @AmountMinor, 'GBP', @ExpenseDate, @Description, SYSUTCDATETIME());
    
    SELECT SCOPE_IDENTITY() AS ExpenseId;
END;
GO

-- Submit expense for approval
CREATE OR ALTER PROCEDURE sp_SubmitExpense
    @ExpenseId INT
AS
BEGIN
    DECLARE @SubmittedStatusId INT;
    SELECT @SubmittedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted';
    
    UPDATE dbo.Expenses
    SET StatusId = @SubmittedStatusId,
        SubmittedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

-- Approve expense
CREATE OR ALTER PROCEDURE sp_ApproveExpense
    @ExpenseId INT,
    @ReviewerId INT
AS
BEGIN
    DECLARE @ApprovedStatusId INT;
    SELECT @ApprovedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Approved';
    
    UPDATE dbo.Expenses
    SET StatusId = @ApprovedStatusId,
        ReviewedBy = @ReviewerId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

-- Reject expense
CREATE OR ALTER PROCEDURE sp_RejectExpense
    @ExpenseId INT,
    @ReviewerId INT
AS
BEGIN
    DECLARE @RejectedStatusId INT;
    SELECT @RejectedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Rejected';
    
    UPDATE dbo.Expenses
    SET StatusId = @RejectedStatusId,
        ReviewedBy = @ReviewerId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

PRINT 'All stored procedures created successfully.';
GO
