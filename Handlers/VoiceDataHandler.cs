using System.Diagnostics;
using System.Runtime.InteropServices;
using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers;

public class VoiceDataHandler : IPacketReceivedHandler
{
    private WaveFormat WaveFormat { get; } = new WaveFormat(48000, 16, 1);
    private WaveOutEvent WaveOut { get; }
    private int FrameSize { get; }
    private int Latency => 20;
    private int DurationMultiplier => 3;
    private short[] PcmBuffer { get; }
    private OpusDecoder Decoder { get; }
    private BufferedWaveProvider BufferedWaveProvider { get; }
    
    public VoiceDataHandler()
    {
        this.WaveOut = new WaveOutEvent();
        this.Decoder = new OpusDecoder(this.WaveFormat.SampleRate, this.WaveFormat.Channels);

        this.FrameSize = this.WaveFormat.SampleRate * this.WaveFormat.Channels * this.Latency / 1000;
        this.PcmBuffer = new short[this.FrameSize * this.DurationMultiplier];
        this.WaveOut.DesiredLatency = this.Latency * this.DurationMultiplier;

        this.BufferedWaveProvider = new BufferedWaveProvider(this.WaveFormat);
        this.BufferedWaveProvider.DiscardOnBufferOverflow = true;
        this.BufferedWaveProvider.BufferDuration = TimeSpan.FromMilliseconds(this.Latency * this.DurationMultiplier);
        this.WaveOut.Init(this.BufferedWaveProvider);
    }
    
    private Stopwatch Stopwatch { get; } = new Stopwatch();

    public void OnPacketReceived(BaseClient baseClient, PacketBase packet)
    {
        if (packet is not VoiceDataPacket voiceDataPacket) return;
        int pcmOffset = 0;
        int encodedOffset = 0;
        this.Stopwatch.Restart();
        for (int i = 0; i < voiceDataPacket.DataOffsets.Length; i++)
        {
            int decoded = this.Decoder.Decode(voiceDataPacket.Data, encodedOffset, voiceDataPacket.DataOffsets[i], this.PcmBuffer, pcmOffset, this.FrameSize, false);
            pcmOffset += decoded;
            encodedOffset += voiceDataPacket.DataOffsets[i];
        }
        //Console.WriteLine($"Decoded {pcmOffset} bytes in {this.Stopwatch.ElapsedMilliseconds}ms");
        Span<byte> pcm = MemoryMarshal.Cast<short, byte>(this.PcmBuffer);
        this.BufferedWaveProvider.AddSamples(pcm.ToArray(), 0, pcm.Length);
        MixingSampleProvider mixingSampleProvider = new MixingSampleProvider(this.WaveFormat);
        this.WaveOut.Play();

    }

    public ushort PacketId => (ushort) PacketType.VoiceData;
}