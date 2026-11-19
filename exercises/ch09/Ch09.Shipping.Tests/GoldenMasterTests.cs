using Ch09.Shipping.Legacy;
using Ch09.Shipping.Refactored;
using Ch09.Shipping.Steps;

namespace Ch09.Shipping.Tests;

// The safety net for the whole refactoring: 11,520 recorded answers of the legacy code
// (golden-master.txt, recorded once with "dotnet run -- record"). Every version must give
// exactly the same answer to every one of them.
public class GoldenMasterTests
{
    private static readonly string[] Golden = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "golden-master.txt"));

    public static TheoryData<string> Versions => new() { "Legacy", "Step1", "Step2", "Step3", "Refactored" };

    private static Func<Samples.Input, string> VersionNamed(string name) => name switch
    {
        "Legacy" => s => new ShippingCalculator().Quote(s.Country, s.Weight, s.Express, s.Member, s.Coupon!, s.Items),
        "Step1" => s => new Step1_ExtractMethod().Quote(s.Country, s.Weight, s.Express, s.Member, s.Coupon!, s.Items),
        "Step2" => s => new Step2_SimplifyConditions().Quote(s.Country, s.Weight, s.Express, s.Member, s.Coupon!, s.Items),
        "Step3" => s => new Step3_ReplaceDuplication().Quote(s.Country, s.Weight, s.Express, s.Member, s.Coupon!, s.Items),
        _ => s => new ShippingQuoter().Quote(s.Country, s.Weight, s.Express, s.Member, s.Coupon!, s.Items),
    };

    [Theory]
    [MemberData(nameof(Versions))]
    public void EveryVersion_GivesTheRecordedAnswer_ForEverySample(string version)
    {
        var quote = VersionNamed(version);
        var inputs = Samples.All().ToList();
        Assert.Equal(Golden.Length, inputs.Count);

        var different = inputs.Select((s, i) => (Now: $"{s} => {quote(s)}", Was: Golden[i]))
                              .Where(p => p.Now != p.Was)
                              .Take(3).ToList();

        Assert.True(different.Count == 0,
            "Different from the golden master:\n" + string.Join("\n", different.Select(d => $"  was: {d.Was}\n  now: {d.Now}")));
    }
}
