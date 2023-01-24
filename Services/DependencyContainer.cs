namespace NetLib.Services;

public class DependencyContainer
{
    private List<Type> _dependencies;

    public void AddDependency(Type type)
    {
        this._dependencies.Add(type);
    }
}