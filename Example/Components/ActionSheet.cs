namespace Example.Components;

using BunnyTail.DependencyInjection;

public interface IActionSheet
{
    ValueTask<string?> ShowAsync(string title, string cancel, params string[] buttons);
}

[Singleton(As = typeof(IActionSheet))]
public sealed class ActionSheet : IActionSheet
{
    public async ValueTask<string?> ShowAsync(string title, string cancel, params string[] buttons)
    {
        var page = Application.Current?.Windows is { Count: > 0 } windows ? windows[^1].Page : null;
        if (page is null)
        {
            return null;
        }

        var result = await page.DisplayActionSheetAsync(title, cancel, null, buttons).ConfigureAwait(true);
        return (result is null) || (result == cancel) ? null : result;
    }
}
