namespace NetLib.Packets;

public interface IPacketMapper<T>
{
    IPacketMapper<T> Register<TPacket>() where TPacket : BasePacket, new();
    
    bool TryGetId<TPacket>(out T? id) where TPacket : BasePacket, new();
    bool TryGetId(Type type, out T? id);
    
    bool TryGetType(T id, out Type? type);
    
    List<Type> GetTypes();
}