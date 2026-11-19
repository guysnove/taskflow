namespace Ch08.Orders.After;

// Chapter 8, AFTER: small interfaces for the things OrderService needs from the outside.
// OrderService depends on these (abstractions), not on the payment company or SMTP (details).
// Each is as small as its users need: Interface Segregation.

public interface IStock
{
    int Available(string sku);                     // -1: no such product
    void Take(string sku, int quantity);
}

public interface IPaymentGateway
{
    bool Charge(string card, decimal amount);      // false: declined
}

public interface INotifier
{
    void OrderPlaced(Customer customer, decimal total);
}

public interface IOrderStore
{
    int Save(Order order, decimal total);          // returns the new order's number
}
