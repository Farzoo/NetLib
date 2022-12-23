using ProtoBuf;
using ProtoBuf.WellKnownTypes;

namespace NetLib.Packets.Shared;

[ProtoContract]
[PacketInfo((ushort)PacketType.VoiceData)]
public class VoiceDataPacket : PacketBase
{
    public override ushort Id => (ushort) PacketType.VoiceData;
    
    [ProtoMember(1)]
    public byte[] Data { get; }
    
    [ProtoMember(2)]
    public int[] DataOffsets { get; }
    
    [ProtoMember(3)]
    public TimeSpan Time { get; }
    
    [ProtoMember(4)]
    public ulong Sequence { get; }
    public VoiceDataPacket(byte[] data, int[] dataOffsets, TimeSpan time, uint sequence)
    {
        this.Data = data;
        this.DataOffsets = dataOffsets;
        this.Time = time;
        this.Sequence = sequence;
    }

    public VoiceDataPacket()
    {
        this.Data = Array.Empty<byte>();
        this.DataOffsets = Array.Empty<int>();
        this.Time = TimeSpan.Zero;
        this.Sequence = 0;
    }
}