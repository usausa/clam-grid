namespace ClamGrid;

public readonly record struct GridIndexRange(int Start, int End)
{
    public int Count => End - Start;
}
