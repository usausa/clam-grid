namespace Example.Behaviors;

public interface IColorSelector
{
    Color? Resolve(object item);
}
