namespace Ch09.Shipping.Refactored;

// Step 4: improve the interface. Six positional parameters, two of them bare bools, became
// one request with names. Compare the two calls:
//   old: calc.Quote("UK", 2.5, true, false, "", 1)        which bool is which?
//   new: quoter.Quote(new("UK", 2.5, Speed.Express))       says what it means
public enum Speed { Standard, Express }

public sealed record ShippingRequest(string Country, double WeightKg, Speed Speed = Speed.Standard,
                                     bool IsMember = false, string? Coupon = null, int Items = 1);

// The answer as data, not as a sentence: callers no longer have to parse "Total: 12.50 (...)".
public sealed record ShippingQuote(double Total, string Days, string? Problem = null)
{
    public bool Available => Problem is null;
}
