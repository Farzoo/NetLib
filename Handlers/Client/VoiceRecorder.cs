using System.Diagnostics;
using System.Runtime.InteropServices;
using Concentus.Enums;
using Concentus.Structs;
using CSCore;
using CSCore.SoundIn;
using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers.Client;

public class VoiceRecorder
{
    private WaveFormat WaveFormat { get; } = new WaveFormat(48000, 16, 1);
    private BaseClient Client { get; }
    private int BufferSize { get; }
    private OpusEncoder Encoder { get; }
    private byte[] Buffer { get; }
    private ushort FrameMultiplier => 5;
    private int FrameDuration => 20;
    private int FrameSize { get; }
    private int[] BufferOffsets { get; }
    private WaveIn WaveIn { get; }
    private Stopwatch PacketStopWatch { get; } = new Stopwatch();

    private TimeSpan LastPacketTimeSpan { get; set; } = TimeSpan.Zero;

    private uint Sequence { get; set; } = 0;
    
    public VoiceRecorder(BaseClient client)
    {
        this.Client = client;
        this.Client.RegisterOnDisconnect(this.OnClientDisconnect);
        
        this.WaveIn = new WaveIn(this.WaveFormat);
        this.WaveIn.DataAvailable += this.WaveIn_DataAvailable;
        this.WaveIn.Latency = this.FrameDuration * this.FrameMultiplier;

        this.BufferSize = this.WaveFormat.SampleRate * this.WaveFormat.Channels * this.FrameDuration * this.FrameMultiplier / 1000;
        this.FrameSize = (int) this.WaveFormat.MillisecondsToBytes(this.FrameDuration * this.FrameMultiplier);
        
        this.Encoder = OpusEncoder.Create(this.WaveFormat.SampleRate, this.WaveFormat.Channels, OpusApplication.OPUS_APPLICATION_AUDIO);
        this.Encoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_AUTO;
        //this.Encoder.Bitrate = 192000;
        this.Encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
        Console.WriteLine($"VBR: {this.Encoder.UseVBR} | VBRConstraint: {this.Encoder.UseConstrainedVBR} | Complexity: {this.Encoder.Complexity} | Bitrate: {this.Encoder.Bitrate} | DTX: {this.Encoder.UseDTX} | FEC: {this.Encoder.UseInbandFEC} | PacketLossPercentage: {this.Encoder.PacketLossPercent} | Mode {this.Encoder.ForceMode} | SignalType: {this.Encoder.SignalType} | Bandwidth: {this.Encoder.Bandwidth}");

        this.Buffer = new byte[this.BufferSize];
        this.BufferOffsets = new int[this.FrameMultiplier];

        this.WaveIn.Initialize();
        this.WaveIn.Start();
    }

    private void WaveIn_DataAvailable(object sender, DataAvailableEventArgs e)
    {
        try
        {
            short[] data = MemoryMarshal.Cast<byte, short>(e.Data).ToArray();
            int offset = 0;
            int frameSizeShort = this.FrameSize / this.FrameMultiplier / 2;
            for (int i = 0; i < this.FrameMultiplier; i++)
            {
                this.BufferOffsets[i] = Encoder.Encode(
                    data, 
                    i * frameSizeShort, 
                    frameSizeShort, this.Buffer,
                    offset, 
                    this.BufferSize - (i * frameSizeShort)
                );
                offset += this.BufferOffsets[i];
            }

            Span<byte> buffer = this.Buffer.AsSpan(0, offset);
            this.Client.SendPacket(
                new VoiceDataPacket(buffer.ToArray(), this.BufferOffsets, this.LastPacketTimeSpan, this.Sequence, this.Client.Id)
            );
            this.LastPacketTimeSpan += TimeSpan.FromMilliseconds(this.WaveIn.Latency);
            
            Console.WriteLine($"Sending voice packet with {buffer.Length} bytes and sequence {this.Sequence} with {this.WaveIn.Latency} ms");
            this.Sequence++;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }
    
    private void OnClientDisconnect(BaseClient client)
    {
        this.WaveIn.Stop();
        this.WaveIn.Dispose();
    }
}