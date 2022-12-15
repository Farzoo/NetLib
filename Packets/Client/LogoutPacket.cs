using NetLib.Packets.Shared;
using ProtoBuf;

namespace NetLib.Packets.Client;

[ProtoContract]
public class LogoutPacket : PacketBase
{
    [ProtoMember(1)]
    public string Reason { get; }
    //public override ushort Size => (ushort) (base.Size + Reason.Length + 1);
    public override ushort Id => (ushort) PacketType.Logout;

    public LogoutPacket()
    {
        this.Reason = "Unknown";
    }
    public LogoutPacket(string reason)
    {
        this.Reason = reason;
    }
}

/*public class LogoutPacketSerializer : PacketSerializer<LogoutPacket>
{
    public override byte[] Serialize(LogoutPacket packetBase)
    {
        byte[] buffer = base.Serialize(packetBase);
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.WriteStringWithTerminator(packetBase.Reason);

        // contatenate the base buffer with the new buffer
        return buffer.Concat(stream.ToArray()).ToArray();
    }

    public override LogoutPacket Deserialize(byte[] data)
    {
        MemoryStream stream = new MemoryStream(data);
        BinaryReader reader = new BinaryReader(stream);
        base.Deserialize(reader, out var packetType);
        if(packetType != PacketType.Logout) throw new PacketDeserializationException("Packet type is not Logout");
        string reason = reader.ReadStringWithTerminator();
        return new LogoutPacket(reason);
    }
}
*/