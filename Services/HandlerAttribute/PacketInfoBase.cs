namespace NetLib.Handlers.HandlerAttribute;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class PacketInfoBase<T> : Attribute
{
    public T Id { get; }

    protected PacketInfoBase(T type)
    {
        this.Id = type;
    }
}