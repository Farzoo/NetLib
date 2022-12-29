using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers.Server;

public class VoiceDataBroadcastHandler : IPacketReceivedHandler
{ 
    private BaseServer Server { get; }
    public ushort PacketId => (ushort) PacketType.VoiceData;
    public VoiceDataBroadcastHandler(BaseServer server)
    {
        this.Server = server;
    }

    public void OnPacketReceived(BaseClient baseClient, PacketBase packet)
    {
        if (packet is not VoiceDataPacket voiceDataPacket) return;

        voiceDataPacket.EntityId = baseClient.Id;
        
        this.Server.Broadcast(voiceDataPacket, baseClient);
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