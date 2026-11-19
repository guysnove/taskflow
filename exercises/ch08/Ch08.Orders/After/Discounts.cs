namespace Ch08.Orders.After;

// Open/Closed: each discount is its own class. A new discount is a new class,
// added to the list in Shop.Create; PriceCalculator never changes.
public interface IDiscountRule
{
    string Name { get; }
    decimal Apply(Order order, decimal total, DateOnly today);   // the new total
}

public class LevelDiscount : IDiscountRule
{
    public string Name => "Customer level";

    public decimal Apply(Order order, decimal total, DateOnly today) => order.Customer.Level switch
    {
        CustomerLevel.Gold => total * 0.90m,       // 10% off
        CustomerLevel.Staff => total * 0.70m,      // 30% off
        _ => total,
    };
}

public class CouponDiscount : IDiscountRule
{
    public string Name => "Coupon SAVE5";

    public decimal Apply(Order order, decimal total, DateOnly today) =>
        order.Coupon == "SAVE5" && total >= 20 ? total - 5 : total;
}

public class BlackFridayDiscount : IDiscountRule
{
    public string Name => "Black Friday";

    // The day after Thanksgiving (the fourth Thursday of November): a Friday from the 23rd to the 29th.
    public decimal Apply(Order order, decimal total, DateOnly today) =>
        IsBlackFriday(today) ? total * 0.80m : total;

    public static bool IsBlackFriday(DateOnly day) =>
        day.Month == 11 && day.DayOfWeek == DayOfWeek.Friday && day.Day >= 23 && day.Day <= 29;
}
