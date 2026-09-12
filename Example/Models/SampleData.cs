namespace Example.Models;

public static class SampleData
{
    public static IEnumerable<SampleRow> CreateRows(int count) =>
        Enumerable.Range(0, count).Select(static id => new SampleRow(id));

    public static ObservableCollection<SampleRow> CreateSource(int count) =>
        [with(CreateRows(count))];
}
