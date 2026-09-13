namespace ClamGrid;

// Direction sequence of repeated sorts on the same key; the None variants remove the key on the third step
public enum GridSortCycle
{
    AscendingDescending,
    DescendingAscending,
    AscendingDescendingNone,
    DescendingAscendingNone
}
