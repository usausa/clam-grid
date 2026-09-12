namespace ClamGrid;

public sealed record GridSortResult(GridSortStatus Status, IReadOnlyList<string> IgnoredKeys, Exception? Error = null);
