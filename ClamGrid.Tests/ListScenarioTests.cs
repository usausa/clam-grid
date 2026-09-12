namespace ClamGrid.Tests;

public sealed class ListScenarioTests
{
    [Fact]
    public void SevenScenariosUseSeventyOneColumnsWithStableKeys()
    {
        // Arrange
        var scenarios = ListScenario.All;

        // Assert
        Assert.Equal(7, scenarios.Count);
        Assert.Equal<int>([18, 17, 18, 18], scenarios.DistinctBy(static scenario => scenario.Family).Select(static scenario => scenario.Columns.Count));
        foreach (var scenario in scenarios)
        {
            Assert.Equal(scenario.Columns.Count, scenario.Columns.Select(static column => column.Key).Distinct(StringComparer.Ordinal).Count());
            Assert.All(scenario.Columns, column => Assert.NotNull(new TicketRow(0).GetText(column.Key)));
        }
    }

    [Theory]
    [InlineData(0, 40)]
    [InlineData(1, 80)]
    [InlineData(2, 120)]
    [InlineData(3, 120)]
    [InlineData(4, 120)]
    [InlineData(5, 120)]
    [InlineData(6, 0)]
    public void BulkSelectionRespectsScenarioCondition(int index, int expected)
    {
        // Arrange
        var scenario = ListScenario.All[index];
        using var view = scenario.CreateData(120);

        // Act
        scenario.SelectAll(view, true);

        // Assert
        Assert.Equal(expected, view.SelectedCount);

        // Act
        scenario.SelectAll(view, false);

        // Assert
        Assert.Equal(0, view.SelectedCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void PendingGroupStaysFirstInBothDirectionsAndCompanyUsesSortValue(int index)
    {
        // Arrange
        var scenario = ListScenario.All[index];
        using var view = scenario.CreateData(120);

        foreach (var descending in new[] { false, true })
        {
            // Act
            view.RestoreSortOrders([new("CompanyName", descending)]);

            // Assert
            Assert.Equal(view.Count(scenario.IsPending), view.TakeWhile(scenario.IsPending).Count());
            var numbers = view.Where(scenario.IsPending).Select(static row => row.CompanyNumber).ToArray();
            Assert.Equal(descending ? numbers.OrderDescending() : numbers.Order(), numbers);
        }
    }

    [Fact]
    public void HeaderColorAndSortMismatchesArePreservedExplicitly()
    {
        // Arrange
        var route = ScenarioColumnCatalog.Escalation.Single(static column => column.Key == "Priority");
        var accept = ScenarioColumnCatalog.Request.Single(static column => column.Key == "PlannedAt");
        using var view = ListScenario.All[3].CreateData(10);

        // Act
        var result = view.RestoreSortOrders(ListScenario.All[3].DefaultKeys.Select(static key => new GridSortOrder(key)));

        // Assert
        Assert.True(route.AllowSorting);
        Assert.False(route.GreenHeader);
        Assert.False(accept.AllowSorting);
        Assert.True(accept.GreenHeader);
        Assert.Equal<string>(["GroupId", "SortOrder"], result.IgnoredKeys);
    }

    [Fact]
    public void StateChangeReappliesPendingSortWithoutLosingSelection()
    {
        // Arrange
        using var view = ListScenario.All[0].CreateData(12);
        var index = view.IndexOfKey(0);
        view.SetSelected(index, true);
        var row = view[index];

        // Act
        row.AdvanceStatus();

        // Assert
        Assert.Same(row, Assert.Single(view.SelectedItems));
        Assert.True(view.IndexOfKey(0) >= 3);
    }
}
