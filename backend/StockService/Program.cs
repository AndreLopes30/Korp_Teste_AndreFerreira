using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StockService.Data;
using StockService.Infrastructure;
using StockService.Services;

var builder = WebApplication.CreateBuilder(args);

var configuredConnectionString = builder.Configuration.GetConnectionString("StockDatabase")
    ?? "Data Source=App_Data/stock.db";
var connectionStringBuilder = new SqliteConnectionStringBuilder(configuredConnectionString);
if (!Path.IsPathRooted(connectionStringBuilder.DataSource))
{
    connectionStringBuilder.DataSource = Path.GetFullPath(
        Path.Combine(builder.Environment.ContentRootPath, connectionStringBuilder.DataSource));
}
Directory.CreateDirectory(Path.GetDirectoryName(connectionStringBuilder.DataSource)!);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(item => item.Value)
    .Where(value => !string.IsNullOrWhiteSpace(value))
    .Cast<string>()
    .ToArray();

builder.Services.AddDbContext<StockDbContext>(options => options.UseSqlite(connectionStringBuilder.ToString()));
builder.Services.AddScoped<StockManagementService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = 400,
            Title = "Dados inválidos",
            Detail = "Revise os campos informados.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["code"] = "validation_error";
        return new BadRequestObjectResult(problem);
    };
});
builder.Services.AddCors(options => options.AddPolicy("AngularClient", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors("AngularClient");
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
