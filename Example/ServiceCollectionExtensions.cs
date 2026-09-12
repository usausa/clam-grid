namespace Example;

using BunnyTail.DependencyInjection;

// 画面と ViewModel は命名規約で登録する（ソース生成）
public static partial class ServiceCollectionExtensions
{
    [ComponentRegistration(Lifetime.Transient, "Page$", Namespace = "Example")]
    [ComponentRegistration(Lifetime.Transient, "View$", Namespace = "Example.Modules")]
    [ComponentRegistration(Lifetime.Transient, "ViewModel$", Namespace = "Example")]
    public static partial IServiceCollection AddViews(this IServiceCollection services);
}
