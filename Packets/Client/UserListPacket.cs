using NetLib.Packets.Shared;
using ProtoBuf;

namespace NetLib.Packets.Client;

[ProtoContract]
public class UserListPacket : PacketBase
{
    public override ushort Id => (ushort) PacketType.Userlist;

    public UserListPacket()
    {
    }
}

/*public class UserListPacketSerialize : PacketSerializer<UserListPacket>
{
    public override UserListPacket Deserialize(byte[] data)
    {
        using MemoryStream stream = new MemoryStream(data);
        using BinaryReader reader = new BinaryReader(stream);
        base.Deserialize(reader, out var packetType);
        if(packetType != PacketType.Userlist)
            throw new PacketDeserializationException("PacketType is not Userlist");
        return new UserListPacket();
    }
}*/