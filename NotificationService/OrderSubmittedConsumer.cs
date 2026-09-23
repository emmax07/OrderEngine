using MassTransit;
using Microsoft.Extensions.Logging;
using SharedContracts;

public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    const string SourceTitle = "NotificationService";
    private readonly ILogger<OrderSubmittedConsumer> _logger;

    public OrderSubmittedConsumer(ILogger<OrderSubmittedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("[{Source}] Processing Order ID: {OrderId} for Customer: {Customer} amounting to ${Amount}", 
            SourceTitle, message.OrderId, message.CustomerId, message.TotalAmount);

        // Simulate background processing delay
        await Task.Delay(500);

        _logger.LogInformation("[{Source}] Successfully processed and dispatched notification for Order ID: {OrderId}", 
            SourceTitle, message.OrderId);
    }
}