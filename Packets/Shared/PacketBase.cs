using ProtoBuf;

namespace NetLib.Packets.Shared;

[ProtoContract]
public abstract class PacketBase
{
    public abstract ushort Id { get; }
    
    public static uint MaxPacketSize = 16384;

    public PacketBase()
    {
    }
}