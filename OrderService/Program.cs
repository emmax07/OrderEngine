using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using SharedContracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); // .NET 9 native OpenAPI registration

// Read PostgreSQL configuration from environment variables or fallback to local defaults
var dbHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "order_db";
var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";

// Fall back to prompting the terminal interactively if the environment variable isn't set
var dbPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
if (string.IsNullOrEmpty(dbPass))
{
    Console.Write("Enter PostgreSQL password for user 'postgres': ");
    dbPass = Console.ReadLine() ?? string.Empty;
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(connectionString));

// Read RabbitMQ configuration from environment variables or fallback to local defaults
var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
var rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_USER") ?? "guest";
var rabbitPass = Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_PASS") ?? "guest";

// Configure MassTransit with RabbitMQ and EF Core Outbox
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Ensure database and outbox tables are created on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.EnsureCreated();
}

// Map native .NET 9 OpenAPI and Swagger UI endpoint
app.MapOpenApi();

app.MapPost("/orders", async (CreateOrderRequest request, OrderDbContext dbContext, IPublishEndpoint publishEndpoint) =>
{
    var orderId = Guid.NewGuid();
    
    var order = new Order
    {
        Id = orderId,
        CustomerId = request.CustomerId,
        TotalAmount = request.TotalAmount,
        Status = "Submitted",
        CreatedAt = DateTime.UtcNow
    };

    // Save entity to PostgreSQL context
    dbContext.Orders.Add(order);

    // Publish event (intercepted safely by EF Outbox)
    var eventMessage = new OrderSubmitted(
        orderId, 
        request.CustomerId, 
        request.TotalAmount, 
        order.CreatedAt
    );

    await publishEndpoint.Publish(eventMessage);
    
    // Commit transaction (persists order and queues message safely)
    await dbContext.SaveChangesAsync();

    return Results.Accepted($"/orders/{orderId}", new { OrderId = orderId, Status = "Submitted" });
})
.WithName("SubmitOrder");

app.Run();

record CreateOrderRequest(string CustomerId, decimal TotalAmount);