using System.Reflection;
using NetLib.Packets.Shared;

namespace NetLib.Packets;

public class PacketMapper : IPacketMapper
{
    private IDictionary<ushort, Type> Mapper { get; } = new Dictionary<ushort, Type>();
    
    public IPacketMapper Register<T>() where T : PacketBase
    {
        if (!this.GetId<T>(out var id)) throw new ArgumentException($"{typeof(T).Name} does not have a PacketInfo attribute.");
        this.Mapper.Add(id!.Value, typeof(T));
        return this;
    }
    
    private bool GetId<T>(out ushort? id) where T : PacketBase
    {
        PacketInfo? packetInfo = typeof(T).GetCustomAttribute<PacketInfo>(false);
        id = packetInfo?.Id;
        return packetInfo != null;
    }
    
    public Type? GetType(ushort id)
    {
        return this.Mapper.TryGetValue(id, out var type) ? type : null;
    }
    
    public List<Type> GetTypes()
    {
        return this.Mapper.Values.ToList();
    }
}

public interface IPacketMapper
{
    IPacketMapper Register<T>() where T : PacketBase;
    Type? GetType(ushort id);
    List<Type> GetTypes();
}