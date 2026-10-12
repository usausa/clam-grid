namespace ClamGrid;

internal static class GridBooleanBox
{
    private static readonly object True = true;

    private static readonly object False = false;

    public static object Box(bool value) => value ? True : False;
}
