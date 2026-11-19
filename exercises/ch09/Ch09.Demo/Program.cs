// Chapter 9 demo.
//   dotnet run -- quotes             a few quotes from the legacy calculator
//   dotnet run -- record <file>      write the golden master: every sample input and the legacy answer
//   dotnet run -- compare <file>     check every version against the golden master
//   dotnet run -- decimal <file>     the same check for the decimal "improvement"
using System.Diagnostics;
using Ch09.Shipping;
using Ch09.Shipping.Experiments;
using Ch09.Shipping.Legacy;
using Ch09.Shipping.Refactored;
using Ch09.Shipping.Steps;

string mode = args.FirstOrDefault() ?? "quotes";
string file = args.Length > 1 ? args[1] : "golden-master.txt";

var versions = new (string Name, Func<Samples.Input, string> Quote)[]
{
    ("Legacy", s => new ShippingCalculator().Quote(s.Country, s.Weight, s.Express, s.Member, s.Coupon!, s.Items)),
    ("Step 1: extract method", s => new Step1_ExtractMethod().Quote(s.Country, s.Weight, s.Express, s.Member, s.Coupon!, s.Items)),
    ("Step 2: simplify conditions", s => new Step2_SimplifyConditions().Quote(s.Country, s.Weight, s.Express, s.Member, s.Coupon!, s.Items)),
    ("Step 3: replace duplication", s => new Step3_ReplaceDuplication().Quote(s.Country, s.Weight, s.Express, s.Member, s.Coupon!, s.Items)),
    ("Step 4: ShippingQuoter", s => new ShippingQuoter().Quote(s.Country, s.Weight, s.Express, s.Member, s.Coupon!, s.Items)),
};

switch (mode)
{
    case "quotes":
        var calc = new ShippingCalculator();
        Console.WriteLine(calc.Quote("US", 2.5, false, false, "", 1));
        Console.WriteLine(calc.Quote("UK", 2.5, true, true, "", 1));
        Console.WriteLine(calc.Quote("DE", 1, false, false, "FREESHIP", 1));
        Console.WriteLine(calc.Quote("JP", 25, false, true, "HALF", 12));
        Console.WriteLine(calc.Quote("BR", 1, true, false, "", 1));
        break;

    case "record":
        var lines = Samples.All().Select(s => $"{s} => {versions[0].Quote(s)}").ToList();
        File.WriteAllLines(file, lines);
        Console.WriteLine($"Recorded {lines.Count:N0} quotes to {Path.GetFileName(file)}");
        break;

    case "compare":
    case "decimal":
        var golden = File.ReadAllLines(file);
        var inputs = Samples.All().ToList();
        var toCheck = mode == "compare" ? versions
            : [("Decimal money", s => new DecimalQuoter().Quote(s.Country, s.Weight, s.Express, s.Member, s.Coupon!, s.Items))];
        foreach (var (name, quote) in toCheck)
        {
            var watch = Stopwatch.StartNew();
            var different = inputs.Select((s, i) => (Line: $"{s} => {quote(s)}", Golden: golden[i]))
                                  .Where(p => p.Line != p.Golden).ToList();
            Console.WriteLine($"{name,-30} {inputs.Count - different.Count,6:N0} same, {different.Count,4} different ({watch.ElapsedMilliseconds} ms)");
            foreach (var d in different.Take(3))
            {
                Console.WriteLine($"    was: {d.Golden}");
                Console.WriteLine($"    now: {d.Line}");
            }
        }
        break;
}
