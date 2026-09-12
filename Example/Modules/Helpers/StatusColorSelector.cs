namespace Example.Modules.Helpers;

using Example.Behaviors;

// 進捗状態で行の背景色を決める。XAMLリソースとして定義する。
public sealed class StatusColorSelector : IColorSelector
{
    public Color? PendingColor { get; set; }

    public Color? ActiveColor { get; set; }

    public Color? CompletedColor { get; set; }

    public Color? Resolve(object item) => item is TicketRow row
        ? row.Status switch { 0 => PendingColor, 1 => ActiveColor, _ => CompletedColor }
        : null;
}
