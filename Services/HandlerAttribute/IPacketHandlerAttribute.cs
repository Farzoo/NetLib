using System.Reflection;
using NetLib.Packets;

namespace NetLib.Handlers.HandlerAttribute;

public interface IPacketHandlerAttribute
{
    Type PacketType { get; }
}

[AttributeUsage(AttributeTargets.Method)]
public abstract class PacketHandlerAttribute : Attribute, IPacketHandlerAttribute
{
    public Type PacketType { get; }

    protected PacketHandlerAttribute(Type packetType)
    {
        if(!typeof(BasePacket).IsAssignableFrom(packetType))
            throw new ArgumentException($"{packetType} must be a subclass of IPacket", nameof(packetType));
        if(packetType.IsAbstract)
            throw new ArgumentException("BasePacket type must not be abstract", nameof(packetType));
        
        this.PacketType = packetType;
    }
}