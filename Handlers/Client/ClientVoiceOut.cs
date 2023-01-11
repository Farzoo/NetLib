using NAudio.Wave;
using NetLib.Packets.Shared;

namespace NetLib.Handlers.Client;

public class ClientVoiceOut : IDisposable
{
    private readonly Guid _clientId;
    private WaveFormat Format { get; }
    private WaveOutEvent WaveOutEvent { get; }
    private VolumeWaveProvider16 VolumeWaveProvider { get; }
    
    private JitterBuffer JitterBuffer { get; }

    private VoiceDataHandler VoiceHandler { get; }

    public ClientVoiceOut(Guid clientId, WaveFormat waveFormat, int latency, int durationMultiplier, float volume = 1.0f)
    {
        this._clientId = clientId;

        this.Format = waveFormat;
        
        this.JitterBuffer = new JitterBuffer(this.Format, latency * durationMultiplier * 4);

        this.VolumeWaveProvider = new VolumeWaveProvider16(this.JitterBuffer)
        {
            Volume = volume
        };

        this.VoiceHandler = new VoiceDataHandler(this.Format, latency, durationMultiplier);

        this.WaveOutEvent = new WaveOutEvent();

        this.WaveOutEvent.Init(this.VolumeWaveProvider);
    }
    
    public void PlayReceivedVoice(VoiceDataPacket packet)
    {
        if(packet.EntityId != this._clientId) return;
        
        Span<byte> decoded = this.VoiceHandler.DecodeVoiceData(packet);
        
        this.JitterBuffer.AddSamples(decoded.ToArray(), 0, decoded.Length, packet.Sequence, packet.Time);
        this.WaveOutEvent.Play();
    }

    private void Stop()
    {
        this.WaveOutEvent.Stop();
        this.WaveOutEvent.Dispose();
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        // Cleanup
        this.Stop();
    }
}