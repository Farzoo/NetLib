using System.Runtime.InteropServices;
using Concentus.Oggfile;
using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers;

public class VoiceDataBroadcastHandler : IPacketReceivedHandler
{
    private BaseServer Server { get; }
    public ushort PacketId => (ushort) PacketType.VoiceData;

    private WaveFormat WaveFormat { get; } = new WaveFormat(48000, 16, 1);
    private int Latency => 20;
    private int DurationMultiplier => 3;
    
    private int JitterLatencyMultiplier => 5;
    private JitterBuffer JitterBuffer { get; }
    private OpusDecoder Decoder { get; }
    private short[] Buffer { get; }
    
    private int FrameSize { get; }

    private Timer? Timer { get; set; }
    
    private IDictionary<BaseClient, JitterBuffer> ClientsJitterBuffers { get; }= new Dictionary<BaseClient, JitterBuffer>();
    
    private MixingSampleProvider MixingSampleProvider { get; }

    public VoiceDataBroadcastHandler(BaseServer server)
    {
        this.Server = server;
        this.JitterBuffer = new JitterBuffer(WaveFormat, Latency * DurationMultiplier);
        this.Decoder = OpusDecoder.Create(WaveFormat.SampleRate, WaveFormat.Channels);
        this.FrameSize = this.WaveFormat.ConvertLatencyToByteSize(this.Latency);
        this.Buffer = new short[this.FrameSize * this.DurationMultiplier];
    }

    public void OnPacketReceived(BaseClient baseClient, PacketBase packet)
    {
        if (packet is not VoiceDataPacket voiceDataPacket) return;
        this.Timer ??= new Timer(this.DataAvailableHandler, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(this.Latency * 15));
        byte[] pcm = this.DecodeVoiceDataPacket(voiceDataPacket).ToArray();
        this.ClientsJitterBuffers.TryGetValue(baseClient, out var jitterBuffer);
        if (jitterBuffer is null)
        {
            jitterBuffer = new JitterBuffer(this.WaveFormat, this.Latency * this.DurationMultiplier * this.JitterLatencyMultiplier);
            this.ClientsJitterBuffers.Add(baseClient, jitterBuffer);
        }
        jitterBuffer.AddSamples(pcm.ToArray(), 0, pcm.Length, voiceDataPacket.Sequence, voiceDataPacket.Time);
    }

    private Span<byte> DecodeVoiceDataPacket(VoiceDataPacket voiceDataPacket)
    {
        int pcmOffset = 0;
        int encodedOffset = 0;
        for (int i = 0; i < voiceDataPacket.DataOffsets.Length; i++)
        {
            int decoded = this.Decoder.Decode(voiceDataPacket.Data, encodedOffset, voiceDataPacket.DataOffsets[i], this.Buffer, pcmOffset, this.FrameSize, false);
            pcmOffset += decoded;
            encodedOffset += voiceDataPacket.DataOffsets[i];
        }
        return MemoryMarshal.Cast<short, byte>(this.Buffer);
    }

    private Span<byte> EncodeVoiceData(byte[] buffer, int offset, int length)
    {
        
    }

    private void DataAvailableHandler(object sender)
    {
        byte[] buffer = new byte[this.FrameSize * this.DurationMultiplier];
        int dataOffset = new int[this.DurationMultiplier];
        foreach (var pair in this.ClientsJitterBuffers)
        {
            for(int i = 0; i < this.DurationMultiplier; i++)
            {
                pair.Value.Read(buffer, this.FrameSize * i, this.FrameSize);
                this.Server.Broadcast(new VoiceDataPacket(buffer, new[] {0}, TimeSpan.Zero, 0));
            }
        }
    } 

}