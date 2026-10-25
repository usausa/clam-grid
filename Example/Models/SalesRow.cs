namespace Example.Models;

public sealed class SalesRow
{
    private const int Days = 30;

    private readonly double baseline;

    private readonly double slope;

    private readonly double target;

    private readonly double[] sales = new double[Days];

    private readonly double[] changes = new double[Days - 1];

    private int day;

    public int Id { get; }

    public string Name { get; }

    public double Today => sales[^1];

    public double Change => (sales[^1] - sales[^2]) / sales[^2];

    public double Achievement { get; private set; }

    public SalesRow(int id)
    {
        Id = id;
        Name = $"店舗 {id + 1:D2}";
        baseline = 200_000 + (Math.Floor(Noise(id, -1) * 30) * 10_000);
        slope = (Noise(id, -2) - 0.5) * 0.02;
        target = baseline * Days * (0.95 + (Noise(id, -3) * 0.35));
        for (var i = 0; i < Days; i++)
        {
            sales[i] = NextSale();
        }

        Update();
    }

    public double[] GetSales() => sales;

    public double[] GetChanges() => changes;

    public void Advance()
    {
        Array.Copy(sales, 1, sales, 0, Days - 1);
        sales[^1] = NextSale();
        Update();
    }

    private static double Noise(int id, int index)
    {
        var hash = unchecked(((uint)id * 73856093u) ^ ((uint)index * 19349663u));
        hash ^= hash >> 13;
        hash = unchecked(hash * 0x5BD1E995u);
        hash ^= hash >> 15;
        return (hash & 0xFFFF) / 65535d;
    }

    private double NextSale()
    {
        var weekend = (day % 7) is 5 or 6 ? 1.3 : 1;
        var trend = 1 + (slope * Math.Min(day, 45));
        var value = baseline * weekend * trend * (0.85 + (Noise(Id, day) * 0.3));
        day++;
        return Math.Round(value);
    }

    private void Update()
    {
        for (var i = 0; i < changes.Length; i++)
        {
            changes[i] = sales[i + 1] - sales[i];
        }

        Achievement = sales.Sum() / target;
    }
}
