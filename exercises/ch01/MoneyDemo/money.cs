// Chapter 1: why double is the wrong type for money.
double d = 12.99 * 2 + 8.50;
decimal m = 12.99m * 2 + 8.50m;
Console.WriteLine($"double : {d:R}");
Console.WriteLine($"decimal: {m}");

double sum = 0;
for (int i = 0; i < 10; i++) sum += 0.10;
Console.WriteLine($"ten x 0.10 as double  = {sum:R}, equals 1.0? {sum == 1.0}");

decimal msum = 0;
for (int i = 0; i < 10; i++) msum += 0.10m;
Console.WriteLine($"ten x 0.10 as decimal = {msum}, equals 1.0? {msum == 1.0m}");

decimal total = 36.0225m;
Console.WriteLine($"rounded to cents: {Math.Round(total, 2, MidpointRounding.AwayFromZero)}");
