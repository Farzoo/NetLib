using System.Reflection;
using NetLib.Handlers;
using NetLib.Handlers.HandlerAttribute;
using NetLib.Packets;

namespace NetLib.Services;

public abstract class PacketServicesManager<TKey, TReceiver, TSender> : IPacketServicesManager<TReceiver, TSender>
    where TReceiver : Delegate
    where TSender : Delegate
{
    
    protected IPacketMapper<TKey> Mapper { get; }
    protected IDictionary<TKey, IList<TReceiver>> ReceivedHandlers { get; } =
        new Dictionary<TKey, IList<TReceiver>>();

    protected IDictionary<TKey, IList<TSender>> SentHandlers { get; } =
        new Dictionary<TKey, IList<TSender>>();

    protected PacketServicesManager(IPacketMapper<TKey> mapper)
    {
        this.Mapper = mapper;
    }

    public IPacketServicesManager<TReceiver, TSender> RegisterPacketHandler<T>(T packetHandler)
    {
        this.InternalRegisterPacketHandler<T, PacketReceiverAttribute, TReceiver>(packetHandler, this.ReceivedHandlers);
        this.InternalRegisterPacketHandler<T, PacketSenderAttribute, TSender>(packetHandler, this.SentHandlers);
        return this;
    }

    private void InternalRegisterPacketHandler<T, U, V>(T packetHandler, IDictionary<TKey, IList<V>> register) 
        where U : PacketHandlerAttribute 
        where V : Delegate 
    {
        var type = typeof(T);

        type.GetMethods().ToList().FindAll(info => info.IsDefined(typeof(U))).ForEach(
            info =>
            {
                if (this.Mapper.TryGetId(info.GetCustomAttribute<U>().PacketType, out var id))
                {
                    Type packetType = info.GetCustomAttribute<U>().PacketType;
                    register.TryGetValue(id!, out var handlers);
                    if (handlers == null)
                    {
                        handlers = new List<V>();
                        register.Add(id!, handlers);
                    }
                    Console.WriteLine($"Registering {info.Name} for {id}");
                    try
                    {
                        handlers.Add((V) Delegate.CreateDelegate(typeof(V), packetHandler, info));
                    }
                    catch (ArgumentException argumentException)
                    {
                        throw new ArgumentException(
                            $"Failed to create delegate handler {info.Name} from class {type} for {packetType}. " +
                            $"The method {info.Name} must have the same signature as the delegate {typeof(V)}. " +
                            $"{info.ReturnType.Name} {info.Name}({info.GetParameters().Select(parameter => parameter.ParameterType.Name).DefaultIfEmpty(string.Empty).Aggregate((p1, p2) => p1 + ", " + p2)}) != " +
                            $"{typeof(V).GetMethod("Invoke")?.ReturnType.Name} {typeof(V).Name}({typeof(V).GetMethod("Invoke")?.GetParameters().Select(parameter => parameter.ParameterType.Name).DefaultIfEmpty(string.Empty).Aggregate((p1, p2) => p1 + ", " + p2)})",
                            argumentException
                        );
                    }
                } else
                {
                    throw new ArgumentException($"Failed to register {info.Name} from class {type} for {typeof(U).Name}.\n" +
                                                $"The method {info.Name} must have the a valid packet ");
                }
            });
    }
}