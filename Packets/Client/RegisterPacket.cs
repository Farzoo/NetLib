using NetLib.Packets.Shared;
using ProtoBuf;

namespace NetLib.Packets.Client;

[ProtoContract]
public class RegisterPacket : PacketBase
{
    [ProtoMember(1)]
    public string Username { get; } 
    [ProtoMember(2)]
    public string Password { get; }
    [ProtoMember(3)]
    public string Email { get; }

    //public override ushort Size => (ushort)(base.Size + Username.Length + Password.Length + Email.Length + 3);
    public override ushort Id => (ushort) PacketType.Register;

    public RegisterPacket()
    {
        this.Username = String.Empty;
        this.Password = String.Empty;
        this.Email = String.Empty;
    }
    public RegisterPacket(string username, string password, string email)
    {
        this.Username = username;
        this.Password = password;
        this.Email = email;
    }
}

/*public class RegisterPacketSerializer : PacketSerializer<RegisterPacket>
{
    public override byte[] Serialize(RegisterPacket packetBase)
    {
        byte[] buffer = base.Serialize(packetBase);
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.WriteStringWithTerminator(packetBase.Username);
        writer.WriteStringWithTerminator(packetBase.Password);
        writer.WriteStringWithTerminator(packetBase.Email);
        
        return buffer.Concat(stream.ToArray()).ToArray();
    }

    public override RegisterPacket Deserialize(byte[] data)
    {
        MemoryStream stream = new MemoryStream(data);
        BinaryReader reader = new BinaryReader(stream);
        base.Deserialize(reader, out var packetType);
        if(packetType != PacketType.ChatMessage) throw new PacketDeserializationException("Packet type is not MessagePacket");
        string username = reader.ReadStringWithTerminator();
        string password = reader.ReadStringWithTerminator();
        string email = reader.ReadStringWithTerminator();
        
        return new RegisterPacket(username, password, email);
    }
}*/