// Chapter 2 case study: the 2008 Zune freeze, translated to C# from the published loop.
// "days" counts days since 1 January 1980 (day 1). The loop works out the year.
// A safety limit stops our copy after 1,000 rounds; the real device had none.
foreach (var date in new[] { new DateOnly(2008, 12, 30), new DateOnly(2008, 12, 31), new DateOnly(2009, 1, 1) })
{
    int days = date.DayNumber - new DateOnly(1980, 1, 1).DayNumber + 1;
    Console.WriteLine($"{date:yyyy-MM-dd} (day {days}): {YearOf(days)}");
}

static string YearOf(int days)
{
    int year = 1980, rounds = 0;
    while (days > 365)
    {
        if (++rounds > 1000) return $"STUCK: still looping with year = {year}, days = {days}";
        if (DateTime.IsLeapYear(year))
        {
            if (days > 366)
            {
                days -= 366;
                year += 1;
            }
        }
        else
        {
            days -= 365;
            year += 1;
        }
    }
    return $"year {year}, day {days} of the year";
}
