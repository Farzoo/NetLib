using NetLib.Packets.Shared;
using ProtoBuf;

namespace ChatAppUtils.Server;

[ProtoContract]
public class ServerChatMessagePacket : PacketBase
{
    [ProtoMember(1)]
    public string Sender { get; }
    [ProtoMember(2)]
    public string Message { get; }

    //public override ushort Size => (ushort) (base.Size + Sender.Length + Message.Length + 2);
    public override ushort Id => (ushort) PacketType.ServerChatMessage;

    public ServerChatMessagePacket()
    {
        this.Sender = string.Empty;
        this.Message = string.Empty;
    }
    public ServerChatMessagePacket(string sender, string message)
    {
        this.Sender = sender;
        this.Message = message;
    }
}