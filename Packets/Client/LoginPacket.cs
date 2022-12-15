using NetLib.Packets.Shared;
using ProtoBuf;

namespace NetLib.Packets.Client;

[ProtoContract]
[PacketInfo((ushort)PacketType.Login)]
public class LoginPacket : PacketBase
{
    [ProtoMember(1)]
    public string Username { get; }
    [ProtoMember(2)]
    public string Password { get; }

    //public override ushort Size => (ushort) (base.Size + Username.Length + Password.Length + 2);
    public override ushort Id => (ushort) PacketType.Login;

    public LoginPacket() : base()
    {
        this.Username = String.Empty;
        this.Password = String.Empty;
    }
    public LoginPacket(string username, string password) : base()
    {
        this.Username = username;
        this.Password = password;
    }
}

/*public class LoginPacketSerializer : PacketSerializer<LoginPacket>
{
    public override byte[] Serialize(LoginPacket packetBase)
    {
        byte[] buffer = base.Serialize(packetBase);
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.WriteStringWithTerminator(packetBase.Username);
        writer.WriteStringWithTerminator(packetBase.Password);
        
        // contatenate the base buffer with the new buffer
        return buffer.Concat(stream.ToArray()).ToArray();
    }

    public override LoginPacket Deserialize(byte[] data)
    {
        using MemoryStream stream = new MemoryStream(data); 
        using BinaryReader reader = new BinaryReader(stream);
        base.Deserialize(reader, out var packetType);
        if(packetType != PacketType.Login) throw new PacketDeserializationException("Packet type is not Login");
        string username = reader.ReadStringWithTerminator();
        string password = reader.ReadStringWithTerminator();
        return new LoginPacket(username, password);
    }
}*/