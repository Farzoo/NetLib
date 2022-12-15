using NetLib.Packets.Shared;
using ProtoBuf;

namespace ChatAppUtils.Server;

[ProtoContract]
public class ServerUserListPacket : PacketBase
{
    [ProtoMember(1)]
    public List<string> Users { get; }
    //public override ushort Size => (ushort) (base.Size + this.Users.Sum(x => x.Length + 1));
    public override ushort Id => (ushort) PacketType.ServerUserlist;

    public ServerUserListPacket()
    {
        this.Users = new List<string>();
    }
    
    public ServerUserListPacket(List<string> users)
    {
        this.Users = users;
    }
}

/*public class ServerUserListPacketSerializer : PacketSerializer<ServerUserListPacket>
{
    public override byte[] Serialize(ServerUserListPacket packetBase)
    { 
        byte[] buffer = base.Serialize(packetBase);
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);
        
        packetBase.Users.ForEach(x => writer.WriteStringWithTerminator(x));
        
        return buffer.Concat(stream.ToArray()).ToArray();
    }

    public override ServerUserListPacket Deserialize(byte[] data)
    {
        MemoryStream stream = new MemoryStream(data);
        BinaryReader reader = new BinaryReader(stream);
        base.Deserialize(reader, out var packetType);
        if(packetType != PacketType.ServerUserlist) throw new PacketDeserializationException("Packet type is not UserlistResult");
        
        List<string> users = new List<string>();
        while(stream.Position < stream.Length)
        {
            users.Add(reader.ReadStringWithTerminator());
        }
        
        return new ServerUserListPacket(users);
    }
}*/

