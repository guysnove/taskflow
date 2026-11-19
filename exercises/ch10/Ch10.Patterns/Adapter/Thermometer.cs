namespace Ch10.Patterns.Adapter;

// Problem: our code needs ITemperature (Celsius). A library we can't change offers something else.

// The library's class (pretend we can't edit it): Fahrenheit, as a string, with a different name.
public sealed class VendorThermometer
{
    public string ReadFahrenheit() => "71.6";
}

// What our code wants.
public interface ITemperature
{
    double Celsius();
}

// ADAPTER: makes VendorThermometer look like ITemperature. All the vendor's quirks stay in here.
public sealed class VendorThermometerAdapter(VendorThermometer vendor) : ITemperature
{
    public double Celsius() =>
        Math.Round((double.Parse(vendor.ReadFahrenheit(), System.Globalization.CultureInfo.InvariantCulture) - 32) * 5 / 9, 1);
}
