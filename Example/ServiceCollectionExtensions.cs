namespace Example;

using BunnyTail.DependencyInjection;

// Views and view models are registered by naming convention (source generated)
public static partial class ServiceCollectionExtensions
{
    [ComponentRegistration(Lifetime.Transient, "Page$", Namespace = "Example")]
    [ComponentRegistration(Lifetime.Transient, "View$", Namespace = "Example.Modules")]
    [ComponentRegistration(Lifetime.Transient, "ViewModel$", Namespace = "Example")]
    public static partial IServiceCollection AddViews(this IServiceCollection services);
}
