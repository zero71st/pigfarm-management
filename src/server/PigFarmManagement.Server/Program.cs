using PigFarmManagement.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS configuration for production
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            // In production, allow Vercel domains
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                ?? new[] { 
                    "https://*.vercel.app", 
                    "https://pigfarm-management-client.vercel.app",  // Update with your actual Vercel URL
                    "https://zero71st-pigfarm-management.vercel.app" // Common Vercel URL pattern
                };
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .SetIsOriginAllowedToAllowWildcardSubdomains(); // Allow wildcard subdomains
        }
    });
});

// Mock in-memory stores
var customers = new List<Customer>
{
    new(Guid.NewGuid(), "C001", "John Farm", CustomerType.Project),
    new(Guid.NewGuid(), "C002", "Mary Farm", CustomerType.Cash),
    new(Guid.NewGuid(), "C003", "Somchai", CustomerType.Project)
};

var pigPens = new List<PigPen>();
var feeds = new List<FeedItem>();
var deposits = new List<Deposit>();
var harvests = new List<HarvestResult>();

PigPenSummary BuildSummary(Guid pigPenId)
{
    var pen = pigPens.First(p => p.Id == pigPenId);
    var penFeeds = feeds.Where(f => f.PigPenId == pigPenId).ToList();
    var penDeposits = deposits.Where(d => d.PigPenId == pigPenId).ToList();
    var penHarvests = harvests.Where(h => h.PigPenId == pigPenId).ToList();

    decimal totalFeed = penFeeds.Sum(f => f.Cost);
    decimal totalDeposit = penDeposits.Sum(d => d.Amount);
    decimal revenue = penHarvests.Sum(h => h.Revenue);
    decimal investment = totalFeed - totalDeposit;
    decimal profitLoss = revenue - totalFeed;
    decimal net = revenue - totalFeed + totalDeposit;

    return new PigPenSummary(pigPenId, totalFeed, totalDeposit, investment, profitLoss, net);
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Production settings
    app.UseHttpsRedirection();
}

// Add health check endpoint for Railway
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// Customers
app.MapGet("/api/customers", () => customers);

// PigPens CRUD (simplified)
app.MapGet("/api/pigpens", () => pigPens.Select(p => p));
app.MapPost("/api/pigpens", (PigPenCreateDto dto) =>
{
    var id = Guid.NewGuid();
    var pen = new PigPen(id, dto.CustomerId, dto.PenCode, dto.PigQty, dto.StartDate, dto.EndDate, dto.EstimatedHarvestDate, 0,0,0);
    pigPens.Add(pen);
    return Results.Created($"/api/pigpens/{id}", pen);
});

app.MapGet("/api/pigpens/{id:guid}/summary", (Guid id) =>
{
    if (!pigPens.Any(p => p.Id == id)) return Results.NotFound();
    return Results.Ok(BuildSummary(id));
});

// Feed add
app.MapPost("/api/pigpens/{id:guid}/feed", (Guid id, FeedCreateDto dto) =>
{
    if (!pigPens.Any(p => p.Id == id)) return Results.NotFound();
    var item = new FeedItem(Guid.NewGuid(), id, dto.FeedType, dto.QuantityKg, dto.PricePerKg, dto.QuantityKg * dto.PricePerKg, dto.Date);
    feeds.Add(item);
    return Results.Ok(item);
});

// Deposit add
app.MapPost("/api/pigpens/{id:guid}/deposit", (Guid id, DepositCreateDto dto) =>
{
    if (!pigPens.Any(p => p.Id == id)) return Results.NotFound();
    var dep = new Deposit(Guid.NewGuid(), id, dto.Amount, dto.Date, dto.Remark);
    deposits.Add(dep);
    return Results.Ok(dep);
});

// Harvest add
app.MapPost("/api/pigpens/{id:guid}/harvest", (Guid id, HarvestCreateDto dto) =>
{
    if (!pigPens.Any(p => p.Id == id)) return Results.NotFound();
    var totalWeight = dto.AvgWeight * dto.PigCount; // simple
    var revenue = totalWeight * dto.SalePricePerKg;
    var harvest = new HarvestResult(Guid.NewGuid(), id, dto.HarvestDate, dto.PigCount, dto.AvgWeight, dto.MinWeight, dto.MaxWeight, totalWeight, dto.SalePricePerKg, revenue);
    harvests.Add(harvest);
    return Results.Ok(harvest);
});

// DTOs internal for now
app.MapGet("/api/pigpens/{id:guid}/feeds", (Guid id) => feeds.Where(f => f.PigPenId == id));
app.MapGet("/api/pigpens/{id:guid}/deposits", (Guid id) => deposits.Where(d => d.PigPenId == id));
app.MapGet("/api/pigpens/{id:guid}/harvests", (Guid id) => harvests.Where(h => h.PigPenId == id));

app.Run();

record PigPenCreateDto(Guid CustomerId, string PenCode, int PigQty, DateTime StartDate, DateTime? EndDate, DateTime? EstimatedHarvestDate);
record FeedCreateDto(string FeedType, decimal QuantityKg, decimal PricePerKg, DateTime Date);
record DepositCreateDto(decimal Amount, DateTime Date, string? Remark);
record HarvestCreateDto(DateTime HarvestDate, int PigCount, decimal AvgWeight, decimal MinWeight, decimal MaxWeight, decimal SalePricePerKg);
