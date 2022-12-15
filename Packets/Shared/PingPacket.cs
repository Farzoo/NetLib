using ProtoBuf;

namespace NetLib.Packets.Shared;

[ProtoContract]
[PacketInfo((ushort)PacketType.Ping)]
public class PingPacket : PacketBase
{
    public PingPacket()
    {
    }

    public override ushort Id => (ushort) PacketType.Ping;
}

/*public class PingPacketSerializer : PacketSerializer<PingPacket>
{
    public override PingPacket Deserialize(byte[] data)
    {
        using MemoryStream stream = new MemoryStream(data);
        using BinaryReader reader = new BinaryReader(stream); 
        base.Deserialize(reader, out var packetType);
        if(packetType != PacketType.Ping)
            throw new PacketDeserializationException("Packet type is not Ping");
        return new PingPacket();
    }
}*/