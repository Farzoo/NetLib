using NetLib.Packets.Shared;
using ProtoBuf;

namespace NetLib.Packets.Client;

[ProtoContract]
public class ChatMessagePacket : PacketBase
{
    [ProtoMember(1)]
    public string Message { get; }
    public override ushort Id => (ushort) PacketType.ChatMessage;

    //public override ushort Size => (ushort) (base.Size + Message.Length + 1);

    public ChatMessagePacket() : base()
    {
        this.Message = "";
    }

    public ChatMessagePacket(string message) : base()
    {
        this.Message = message;
    }
}

/*public class ChatMessagePacketSerializer : PacketSerializer<ChatMessagePacket>
{
    public override byte[] Serialize(ChatMessagePacket packetBase)
    {
        byte[] buffer = base.Serialize(packetBase);
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);

        writer.WriteStringWithTerminator(packetBase.Message);

        // contatenate the base buffer with the new buffer
        return buffer.Concat(stream.ToArray()).ToArray();
    }
    
    public override ChatMessagePacket Deserialize(byte[] data)
    {
        MemoryStream stream = new MemoryStream(data);
        BinaryReader reader = new BinaryReader(stream);
        base.Deserialize(reader, out var packetType);
        if(packetType != PacketType.ChatMessage) throw new PacketDeserializationException("Packet type is not MessagePacket");
        string message = reader.ReadStringWithTerminator();
        return new ChatMessagePacket(message);
    }
    
}*/