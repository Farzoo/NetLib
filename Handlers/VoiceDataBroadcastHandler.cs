using System.Collections.Concurrent;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers;

public class VoiceDataBroadcastHandler : IPacketReceivedHandler
{ 
    private BaseServer Server { get; }
    public ushort PacketId => (ushort) PacketType.VoiceData;
    public int Latency { get; }
    public int DurationMultiplier { get; }
    public WaveFormat WaveFormat { get; }
    private Timer BroadcastTimer { get; }
    
    private VoiceDataHandler VoiceDataHandler { get; }

    //private IDictionary<BaseClient, JitterBuffer> ClientsJitterBuffers { get; }= new Dictionary<BaseClient, JitterBuffer>();
    private IDictionary<BaseClient, VoiceDataHandler> ClientsVoiceDataHandlers { get; } = new ConcurrentDictionary<BaseClient, VoiceDataHandler>();
    private IDictionary<BaseClient, JitterBuffer> ClientsJitterBuffers { get; } = new ConcurrentDictionary<BaseClient, JitterBuffer>();
    private IDictionary<BaseClient, ISampleProvider> ClientsJitterBuffersWrapper { get; } = new ConcurrentDictionary<BaseClient, ISampleProvider>();
    private ISet<BaseClient> Clients { get; } = new HashSet<BaseClient>();
    private ContinuousMixingSampleProvider MixingSampleProvider { get; }
    private IWaveProvider MixingWaveProvider { get; }
    private byte[] MixingBuffer { get; }
    
    private SemaphoreSlim ClientsLock { get; } = new SemaphoreSlim(1, 1);

    private TimeSpan TimeSinceFirstPacket { get; set; } = TimeSpan.Zero;
    
    private uint SequenceNumber { get; set; } = 0;
    
    private bool IsFirstPacket { get; set; } = true;

    public VoiceDataBroadcastHandler(BaseServer server)
    {
        this.Server = server;
        this.Latency = 20;
        this.DurationMultiplier = 3;
        this.WaveFormat = new WaveFormat(48000, 16, 1);
        this.BroadcastTimer = new Timer(this.BroadcastAudio, null, Timeout.Infinite, this.Latency * this.DurationMultiplier);
        this.MixingSampleProvider = new ContinuousMixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(this.WaveFormat.SampleRate, this.WaveFormat.Channels));
        this.MixingWaveProvider = SampleProviderConverters.ConvertSampleProviderIntoWaveProvider(this.MixingSampleProvider, 16);
        this.MixingBuffer = new byte[this.WaveFormat.ConvertLatencyToByteSize(this.Latency * this.DurationMultiplier)];
        this.VoiceDataHandler = new VoiceDataHandler(this.WaveFormat, this.Latency, this.DurationMultiplier);
    }

    public void OnPacketReceived(BaseClient baseClient, PacketBase packet)
    {
        if (packet is not VoiceDataPacket voiceDataPacket) return;
        
        if (!this.Clients.Contains(baseClient)) this.SetupClient(baseClient);

        //Console.WriteLine($"Receiving packet from {baseClient.Id} with sequence number {voiceDataPacket.Sequence} and {voiceDataPacket.Data.Length} bytes of data");
        Span<byte> pcm = this.ClientsVoiceDataHandlers[baseClient].DecodeVoiceData(voiceDataPacket);

        JitterBuffer jb = this.ClientsJitterBuffers[baseClient];
        
        lock(jb)
        {
            jb.AddSamples(pcm.ToArray(), 0, pcm.Length, voiceDataPacket.Sequence, voiceDataPacket.Time);
        }
        
        if (this.IsFirstPacket)
        {
            this.BroadcastTimer.Change(0, this.Latency * this.DurationMultiplier);
            this.IsFirstPacket = false;
        }
    }

    private void BroadcastAudio(object state)
    {
        this.ClientsLock.Wait();
        int samplesLength = this.WaveFormat.ConvertLatencyToByteSize(this.Latency * this.DurationMultiplier);
        
        foreach (BaseClient client in this.Clients)
        {
            ISampleProvider clientSampleProvider = this.ClientsJitterBuffersWrapper[client];
            JitterBuffer jb = this.ClientsJitterBuffers[client];
            
            this.MixingSampleProvider.RemoveMixerInput(clientSampleProvider);
            int length;
            lock (jb)
            { 
                length = this.MixingWaveProvider.Read(this.MixingBuffer, 0, this.MixingBuffer.Length);
            }
            
            // Count number of zeros read
            int zeros = 0;
            for (int i = 0; i < length; i++)
            {
                if (this.MixingBuffer[i] == 0) zeros++;
            }
            
            //Console.WriteLine($"Read {length} bytes from mixing buffer with {zeros} bytes for silence");

            Span<byte> data = this.VoiceDataHandler.EncodeVoiceData(this.MixingBuffer.AsSpan(), out var offsets);
            
            client.SendPacket(new VoiceDataPacket(data.ToArray(), offsets, this.TimeSinceFirstPacket, this.SequenceNumber));
           
            Array.Clear(this.MixingBuffer, 0, length);
            this.MixingSampleProvider.AddMixerInput(clientSampleProvider);
        }
        
        this.ClientsJitterBuffers.Values.ToList().ForEach(buffer => buffer.Clear(this.MixingBuffer.Length));

        this.TimeSinceFirstPacket += TimeSpan.FromMilliseconds(this.Latency * this.DurationMultiplier);
        this.SequenceNumber++;
        
        this.ClientsLock.Release();
    }
    
    private void ClientDisconnected(BaseClient client)
    {
        this.Clients.Remove(client);
        
        this.ClientsVoiceDataHandlers.Remove(client);
        
        this.ClientsJitterBuffers.Remove(client);
        
        this.MixingSampleProvider.RemoveMixerInput(this.ClientsJitterBuffersWrapper[client]);
        this.ClientsJitterBuffersWrapper.Remove(client);
    }

    private void SetupClient(BaseClient baseClient)
    {
        this.ClientsLock.Wait();
        if (!this.Clients.Contains(baseClient))
        {
            baseClient.RegisterOnDisconnect(this.ClientDisconnected);

            this.ClientsVoiceDataHandlers.Add(baseClient,
                new VoiceDataHandler(this.WaveFormat, this.Latency, this.DurationMultiplier));

            this.ClientsJitterBuffers.Add(baseClient,
                new JitterBuffer(this.WaveFormat, this.Latency * this.DurationMultiplier * 5));

            ISampleProvider sampleProvider = SampleProviderConverters.ConvertWaveProviderIntoSampleProvider(this.ClientsJitterBuffers[baseClient]);
            this.ClientsJitterBuffersWrapper.Add(baseClient, sampleProvider);
            this.MixingSampleProvider.AddMixerInput(sampleProvider);
            this.Clients.Add(baseClient);
        }
        this.ClientsLock.Release();
    }
}

public static class SampleProviderConverters
{
    public static ISampleProvider ConvertWaveProviderIntoSampleProvider(IWaveProvider waveProvider)
    {
        if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
        {
            if (waveProvider.WaveFormat.BitsPerSample == 8)
                return new Pcm8BitToSampleProvider(waveProvider);
            if (waveProvider.WaveFormat.BitsPerSample == 16)
                return new Pcm16BitToSampleProvider(waveProvider);
            if (waveProvider.WaveFormat.BitsPerSample == 24)
                return new Pcm24BitToSampleProvider(waveProvider);
            if (waveProvider.WaveFormat.BitsPerSample == 32)
                return new Pcm32BitToSampleProvider(waveProvider);
            throw new InvalidOperationException("Unsupported bit depth");
        }
        if (waveProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            throw new ArgumentException("Unsupported source encoding");
        return waveProvider.WaveFormat.BitsPerSample == 64 ? new WaveToSampleProvider64(waveProvider) : new WaveToSampleProvider(waveProvider);
    }
    
    public static IWaveProvider ConvertSampleProviderIntoWaveProvider(ISampleProvider sampleProvider, int bitDepth)
    {
        if(sampleProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            throw new ArgumentException("Unsupported source encoding");
        
        return bitDepth switch
        {
            8  => new SampleToWaveProvider(sampleProvider),
            16 => new SampleToWaveProvider16(sampleProvider),
            24 => new SampleToWaveProvider24(sampleProvider),
            _  => throw new InvalidOperationException("Unsupported bit depth")
        };
    }
}