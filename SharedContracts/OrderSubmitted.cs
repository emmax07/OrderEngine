namespace SharedContracts;

public record OrderSubmitted(
    Guid OrderId,
    string CustomerId,
    decimal TotalAmount,
    DateTime Timestamp
);

