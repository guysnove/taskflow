// BrightCart order processor. Reads orders.csv, works out totals, writes invoices, "emails" customers.
// NOTE: a fictitious example written for Chapter 1, in the style of real legacy code. It has problems on purpose.
using System.Text;

class Program
{
    static List<string[]> data = new List<string[]>();
    static double total = 0;
    static int count = 0;

    static void Main(string[] args)
    {
        var lines = File.ReadAllLines("orders.csv");
        for (int i = 1; i < lines.Length; i++)
        {
            // split the line, but keep the quoted items together
            var l = lines[i];
            var q1 = l.IndexOf('"');
            var q2 = l.LastIndexOf('"');
            var items = l.Substring(q1 + 1, q2 - q1 - 1);
            var before = l.Substring(0, q1 - 1).Split(',');
            var after = l.Substring(q2 + 2);
            data.Add(new string[] { before[0], before[1], before[2], before[3], items, after });
        }

        foreach (var d in data)
        {
            try
            {
                double sub = 0;
                var sb = new StringBuilder();
                sb.AppendLine("BrightCart - Invoice " + d[0]);
                sb.AppendLine("Customer: " + d[1] + " (" + d[3] + ")");
                foreach (var it in d[4].Split(';'))
                {
                    var p = it.Split(':');
                    var price = double.Parse(p[1]);
                    var qty = int.Parse(p[2]);
                    sub = sub + price * qty;
                    sb.AppendLine("  " + p[0] + " x" + qty + " @ " + price + " = " + (price * qty));
                }
                double disc = 0;
                if (d[3] == "VIP") disc = sub * 0.1;
                if (sub > 100) disc = disc + sub * 0.1;   // big orders get 10% too
                double ship = 4.99;
                if (sub > 50) ship = 0;
                var tot = sub - disc + ship;
                sb.AppendLine("  Subtotal: " + sub);
                sb.AppendLine("  Discount: -" + disc);
                if (sub - disc > 60) sb.AppendLine("  Shipping: FREE"); else sb.AppendLine("  Shipping: " + ship);
                sb.AppendLine("  TOTAL: " + tot);
                Console.WriteLine(sb.ToString());
                File.WriteAllText("invoice-" + d[0] + ".txt", sb.ToString());
                Console.WriteLine("Sending email to " + d[2] + "...");
                total = total + tot;
                count++;
            }
            catch
            {
            }
        }
        Console.WriteLine("Processed " + count + " orders, revenue " + total);
    }
}
