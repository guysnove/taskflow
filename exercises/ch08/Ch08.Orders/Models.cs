namespace Ch08.Orders;

// The shop's data. Both versions (Before and After) use the same models.
public enum CustomerLevel { Regular, Gold, Staff }

public sealed record Customer(int Id, string Name, string Email, CustomerLevel Level);

public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);

// Card is the card number the customer typed. 4000... is a card that's always declined.
public sealed record Order(Customer Customer, IReadOnlyList<OrderLine> Lines,
                           string Card = "4242424242424242", string? Coupon = null);
