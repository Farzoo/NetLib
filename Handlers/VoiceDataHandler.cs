using System.Diagnostics;
using System.Runtime.InteropServices;
using Concentus.Enums;
using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NetLib.Handlers.Server;
using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers;

public class VoiceDataHandler
{
    private WaveFormat WaveFormat { get; }
    public int FrameSize { get; }
    public int Latency { get; }
    public int DurationMultiplier { get; }
    
    private short[] PcmBuffer { get; }
    private byte[] OpusBuffer { get; }
    
    private OpusDecoder Decoder { get; }
    private OpusEncoder Encoder { get; }

    public VoiceDataHandler(WaveFormat waveFormat, int latency, int durationMultiplier)
    {
        this.WaveFormat = waveFormat;
        this.Latency = latency;
        this.DurationMultiplier = durationMultiplier;
        
        this.FrameSize = this.WaveFormat.ConvertLatencyToByteSize(this.Latency);
        
        this.PcmBuffer = new short[this.FrameSize * this.WaveFormat.ConvertLatencyToByteSize(this.DurationMultiplier)];
        this.OpusBuffer = new byte[this.FrameSize * this.WaveFormat.ConvertLatencyToByteSize(this.DurationMultiplier)];

        this.Decoder = OpusDecoder.Create(this.WaveFormat.SampleRate, this.WaveFormat.Channels);
        this.Encoder = OpusEncoder.Create(this.WaveFormat.SampleRate, this.WaveFormat.Channels, OpusApplication.OPUS_APPLICATION_VOIP);
    }

    /**
     * Returns a span of the encoded voice data. The span is a view on the internal buffer, so it is only valid until the next call to this method.
     * No memory allocation is done for each call.
     */
    public Span<byte> DecodeVoiceData(VoiceDataPacket voiceDataPacket)
    {
        int pcmOffset = 0;
        int opusOffset = 0;
        for (int i = 0; i < voiceDataPacket.DataOffsets.Length; i++)
        {
            pcmOffset += this.Decoder.Decode(voiceDataPacket.Data, opusOffset, voiceDataPacket.DataOffsets[i],
                this.PcmBuffer, pcmOffset, this.FrameSize);
            opusOffset += voiceDataPacket.DataOffsets[i];
        }
        return MemoryMarshal.Cast<short, byte>(this.PcmBuffer.AsSpan(0, pcmOffset));
    }
    
    /**
     * Returns a span of the encoded voice data. The span is a view on the internal buffer, so it is only valid until the next call to this method.
     * No memory allocation is done for each call.
     */
    public Span<byte> EncodeVoiceData(Span<byte> pcm, out int[] offsets)
    {
        short[] data = MemoryMarshal.Cast<byte, short>(pcm).ToArray();
        int offset = 0;
        int pcmSliceSize = this.FrameSize / 2; // We need to divide by 2 because we are working with shorts
        int[] bufferOffsets = new int[this.DurationMultiplier];
        
        Array.Clear(this.OpusBuffer, 0, this.OpusBuffer.Length);
        Console.WriteLine(pcmSliceSize);
        
        for (int i = 0; i < this.DurationMultiplier; i++)
        {
            bufferOffsets[i] = Encoder.Encode(
                data, 
                i * pcmSliceSize, 
                pcmSliceSize, this.OpusBuffer,
                offset, 
                this.OpusBuffer.Length - (i * pcmSliceSize)
            );
            offset += bufferOffsets[i];
        }

        offsets = bufferOffsets;
        return this.OpusBuffer.AsSpan(0, offset);
    }
}

public class VoiceDataClientHandler : IPacketReceivedHandler
{
    private VoiceDataHandler VoiceDataHandler { get; }
    private WaveFormat WaveFormat { get; }
    private WaveOutEvent WaveOut { get; }
    
    private int Latency { get; }
    private int DurationMultiplier { get; }
    
    private BufferedWaveProvider BufferedWaveProvider { get; }

    public VoiceDataClientHandler(WaveFormat waveFormat, int latency, int durationMultiplier)
    {
        this.WaveFormat = waveFormat;
        this.Latency = latency;
        this.DurationMultiplier = durationMultiplier;
        this.VoiceDataHandler = new VoiceDataHandler(this.WaveFormat, this.Latency, this.DurationMultiplier);
        this.BufferedWaveProvider = new BufferedWaveProvider(this.WaveFormat);
        this.BufferedWaveProvider.BufferDuration = TimeSpan.FromMilliseconds(this.VoiceDataHandler.Latency * this.VoiceDataHandler.DurationMultiplier*2);
        this.BufferedWaveProvider.BufferLength = this.VoiceDataHandler.FrameSize * this.VoiceDataHandler.DurationMultiplier * 5;
        this.WaveOut = new WaveOutEvent();
        this.WaveOut.Init(this.BufferedWaveProvider);
    }
    public void OnPacketReceived(BaseClient baseClient, PacketBase packet)
    {
        if(packet is not VoiceDataPacket voiceDataPacket) return;
        //Console.WriteLine($"Received voice data packet with {voiceDataPacket.Data.Length} and sequence {voiceDataPacket.Sequence}");
        byte[] pcm = this.VoiceDataHandler.DecodeVoiceData(voiceDataPacket).ToArray();
        this.BufferedWaveProvider.AddSamples(pcm, 0, pcm.Length);
        this.WaveOut.Play();
    }

    public ushort PacketId => (ushort) PacketType.VoiceData;
}