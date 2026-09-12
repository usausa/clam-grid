namespace ClamGrid.Tests;

public sealed class ScenarioSortCallbackTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void CallbackMatchesDefaultSortingForEveryBusinessKeyAndBothDirections(int index)
    {
        // Arrange
        var scenario = ListScenario.All[index];
        using var standard = scenario.CreateData(300);
        using var callback = scenario.CreateData(300, true);

        // Assert
        Verify();

        foreach (var key in scenario.GetSortKeys())
        {
            for (var direction = 0; direction < 2; direction++)
            {
                // Act & Assert
                Assert.Equal(GridSortStatus.Applied, standard.SortBy(key).Status);
                Assert.Equal(GridSortStatus.Applied, callback.SortBy(key).Status);
                Verify();
            }
        }

        // Act
        standard[standard.IndexOfKey(42)].AdvanceStatus();
        callback[callback.IndexOfKey(42)].AdvanceStatus();

        // Assert
        Verify();

        // Act
        standard.RestoreSortOrders([]);
        callback.RestoreSortOrders([]);

        // Assert
        Verify();

        void Verify()
        {
            Assert.Equal(standard.SortOrders, callback.SortOrders);
            Assert.Equal(standard.Select(static row => row.Id), callback.Select(static row => row.Id));
        }
    }
}
