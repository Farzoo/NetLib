using ProtoBuf;

namespace NetLib.Packets.Shared;

[ProtoContract]
[PacketInfo((ushort)PacketType.VoiceData)]
public class VoiceDataPacket : PacketBase
{
    public override ushort Id => (ushort) PacketType.VoiceData;
    
    [ProtoMember(1)]
    public byte[] Data { get; }

    public VoiceDataPacket(byte[] data)
    {
        this.Data = data;
    }
    
    public VoiceDataPacket()
    {
        this.Data = Array.Empty<byte>();
    }
}