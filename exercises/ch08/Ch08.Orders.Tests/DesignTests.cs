using Ch08.Orders.After;

namespace Ch08.Orders.Tests;

public class PaymentTests
{
    // Liskov broken: code written for PaymentMethod crashes on one of its subclasses.
    [Fact]
    public void Bad_RefundAll_CrashesOnAGiftCard()
    {
        var card = new Payments.Bad.CardPayment();
        var gift = new Payments.Bad.GiftCardPayment();

        var ex = Assert.Throws<NotSupportedException>(() => Payments.Bad.Refunds.RefundAll([card, gift], 10m));

        Assert.Equal("Gift card payments can't be refunded.", ex.Message);
        Assert.Equal(["card refunded 10.00"], card.History);   // half done: the card WAS refunded
    }

    [Fact]
    public void Good_RefundWherePossible_SaysWhatWasNotRefunded()
    {
        var card = new Payments.Good.CardPayment();
        var gift = new Payments.Good.GiftCardPayment();

        var left = Payments.Good.Refunds.RefundWherePossible([card, gift], 10m);

        Assert.Equal(["card refunded 10.00"], card.History);
        Assert.Same(gift, Assert.Single(left));
    }
}

public class NotifierTests
{
    private static readonly Customer Ana = new(1, "Ana", "ana@example.com", CustomerLevel.Regular);

    [Fact]
    public void Logging_WrapsAnyNotifier()
    {
        var inner = new RecordingNotifier();
        var log = new StringWriter();

        new LoggingNotifier(inner, log).OrderPlaced(Ana, 17m);

        Assert.Contains("notifying customer 1 with RecordingNotifier", log.ToString());
        Assert.Single(inner.Sent);
    }

    [Fact]
    public void AllNotifiers_TellsEachOne()
    {
        var a = new RecordingNotifier();
        var b = new RecordingNotifier();

        new AllNotifiers(a, b).OrderPlaced(Ana, 17m);

        Assert.Single(a.Sent);
        Assert.Single(b.Sent);
    }

    [Fact]
    public void Overdone_AndSimple_SayTheSameThing()
    {
        var formatter = new Overdone.DefaultThankYouMessageFormatterFactory().Create();
        string overdone = formatter.Format(new Overdone.DefaultThankYouMessageContext("Ana"));

        Assert.Equal(Overdone.ThankYou.For("Ana"), overdone);
    }
}
