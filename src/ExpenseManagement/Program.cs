using ExpenseManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Expense Management API", Version = "v1" });
});

// Register expense service with fallback to dummy service
builder.Services.AddScoped<IExpenseService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<ExpenseService>>();
    
    try
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("YOURSERVER"))
        {
            return new DummyExpenseService(sp.GetRequiredService<ILogger<DummyExpenseService>>());
        }
        return new ExpenseService(configuration, logger);
    }
    catch
    {
        return new DummyExpenseService(sp.GetRequiredService<ILogger<DummyExpenseService>>());
    }
});

// Register chat service
builder.Services.AddScoped<IChatService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<ChatService>>();
    var expenseService = sp.GetRequiredService<IExpenseService>();
    
    var endpoint = configuration["OpenAI:Endpoint"];
    if (string.IsNullOrEmpty(endpoint))
    {
        return new DummyChatService();
    }
    
    return new ChatService(configuration, logger, expenseService);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Management API v1");
    c.RoutePrefix = "swagger";
});

app.MapRazorPages();
app.MapControllers();

app.Run();
