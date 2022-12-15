using ProtoBuf;

namespace NetLib.Packets.Shared;

[ProtoContract]
public class UnknownPacket : PacketBase
{
    public UnknownPacket() 
    {
    }

    public override ushort Id => (ushort) PacketType.Unknown;
}