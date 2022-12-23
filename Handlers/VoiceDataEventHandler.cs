﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using Concentus.Enums;
using Concentus.Structs;
using CSCore;
using CSCore.SoundIn;
using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers;

public class VoiceDataEventHandler
{
    private WaveFormat WaveFormat { get; } = new WaveFormat(48000, 16, 1);
    private BaseClient Client { get; }
    private int BufferSize { get; }
    private OpusEncoder Encoder { get; }
    private byte[] Buffer { get; }
    private ushort FrameMultiplier => 3;
    private int FrameDuration => 20;
    private int FrameSize { get; }
    private int[] BufferOffsets { get; }
    private WaveIn WaveIn { get; }
    private Stopwatch PacketStopWatch { get; } = new Stopwatch();

    private TimeSpan LastPacket { get; set; } = TimeSpan.Zero;

    private uint Sequence { get; set; } = 0;
    
    public VoiceDataEventHandler(BaseClient client)
    {
        this.Client = client;
        this.WaveIn = new WaveIn(this.WaveFormat);
        this.WaveIn.DataAvailable += this.WaveIn_DataAvailable;
        this.WaveIn.Latency = this.FrameDuration * this.FrameMultiplier;

        this.BufferSize = this.WaveFormat.SampleRate * this.WaveFormat.Channels * this.FrameDuration * this.FrameMultiplier / 1000;
        this.FrameSize = this.WaveFormat.SampleRate * this.WaveFormat.Channels * this.FrameDuration / 1000;
        
        Console.WriteLine(this.BufferSize);
        this.Encoder = OpusEncoder.Create(this.WaveFormat.SampleRate, this.WaveFormat.Channels, OpusApplication.OPUS_APPLICATION_AUDIO);
        this.Encoder.Bandwidth = OpusBandwidth.OPUS_BANDWIDTH_AUTO;
        this.Encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
        this.Encoder.UseVBR = true;
        this.Encoder.UseDTX = true;
        Console.WriteLine($"VBR: {this.Encoder.UseVBR} | VBRConstraint: {this.Encoder.UseConstrainedVBR} | Complexity: {this.Encoder.Complexity} | Bitrate: {this.Encoder.Bitrate} | DTX: {this.Encoder.UseDTX} | FEC: {this.Encoder.UseInbandFEC} | PacketLossPercentage: {this.Encoder.PacketLossPercent} | Mode {this.Encoder.ForceMode} | SignalType: {this.Encoder.SignalType} | Bandwidth: {this.Encoder.Bandwidth}");

        this.Buffer = new byte[this.BufferSize];
        this.BufferOffsets = new int[this.FrameMultiplier];

        this.WaveIn.Initialize();
        this.WaveIn.Start();
        this.PacketStopWatch.Start();
    }

    private void WaveIn_DataAvailable(object sender, DataAvailableEventArgs e)
    {
        try
        {
            short[] data = MemoryMarshal.Cast<byte, short>(e.Data).ToArray();
            int offset = 0;
            for (int i = 0; i < this.FrameMultiplier; i++)
            {
                this.BufferOffsets[i] = Encoder.Encode(
                    data, 
                    i * this.FrameSize, 
                    this.FrameSize, this.Buffer,
                    offset, 
                    this.BufferSize - (i * this.FrameSize)
                );
                offset += this.BufferOffsets[i];
            }

            Span<byte> buffer = this.Buffer.AsSpan(0, offset);
            this.Client.SendPacket(
                new VoiceDataPacket(buffer.ToArray(), this.BufferOffsets, TimeSpan.FromMilliseconds(this.PacketStopWatch.ElapsedMilliseconds), this.Sequence)
            );
            Console.WriteLine($"Sending voice packet with {buffer.Length} bytes and sequence {this.Sequence} at {this.PacketStopWatch.ElapsedMilliseconds} ms");
            this.Sequence++;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }
}