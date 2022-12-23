
using NAudio.Utils;
using NAudio.Wave;
using WaveFormat = NAudio.Wave.WaveFormat;

namespace NetLib.Handlers;

public class JitterBuffer : IWaveProvider
{
    private TimeSpan EndOfLastPacket { get; set; }
    private ulong LastPacketSequenceNumber { get; set; }
    private CircularBuffer CircularBuffer { get; }

    private readonly IDictionary<ulong, SamplesHolder> _earlySamples = new SortedDictionary<ulong, SamplesHolder>();
    public WaveFormat WaveFormat { get; }
    public int Latency { get; }
    private int FrameSize { get; } 
    private int Count { get; set; } = 0;
    private SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);
    private bool IsReset { get; set; } = true;
    public JitterBuffer(WaveFormat waveFormat, int latency)
    {
        this.WaveFormat = waveFormat;
        this.FrameSize = this.ComputeFrameSize(latency);
        this.CircularBuffer = new CircularBuffer(2 * this.FrameSize);
        this.Latency = latency;
    }

    public int ComputeFrameSize(int latency)
    {
        return this.WaveFormat.ConvertLatencyToByteSize(latency);
    }

    public TimeSpan ComputeBufferDuration(byte[] buffer, int offset, int count)
    {
        int length = offset + count > buffer.Length ? buffer.Length - offset : count;
        if(length % this.WaveFormat.BlockAlign != 0)
        {
            throw new ArgumentException("Buffer length is not valid for this bits depth.");
        }
        return TimeSpan.FromMilliseconds(this.WaveFormat.SampleRate * this.WaveFormat.Channels / 1000.0 / length);
    }

    public void AddSamples(byte[] data, int offset, int length, ulong sequence, TimeSpan timeSpan)
    {
        this.Lock.Wait();
        if(this.IsReset)
        {
            this.IsReset = false;
            this.EndOfLastPacket = timeSpan + this.ComputeBufferDuration(data, offset, length);
            this.LastPacketSequenceNumber = sequence;
            this.Count += this.CircularBuffer.Write(data, offset, length);
        }
        else if (sequence > this.LastPacketSequenceNumber + 1)
        {
            if(!this._earlySamples.ContainsKey(sequence)) this._earlySamples.Add(sequence, new SamplesHolder(data.ToArray(), offset, length, timeSpan, sequence));
        }
        else if (sequence == this.LastPacketSequenceNumber + 1)
        {
            this.AddSamplesSimple(data, offset, length, sequence, timeSpan);
            this.ReorganizeBuffer();
        }
        this.Lock.Release();
    }

    private void AddSamplesSimple(byte[] data, int offset, int length, ulong sequence, TimeSpan timeSpan)
    {
        int circularBufferOffset = this.WaveFormat.ConvertLatencyToByteSize((int)(timeSpan - this.EndOfLastPacket).TotalMilliseconds);
        if (circularBufferOffset >= this.FrameSize || timeSpan - this.EndOfLastPacket < TimeSpan.Zero) return;
        this.CircularBuffer.Advance(this.WaveFormat.ConvertLatencyToByteSize(circularBufferOffset));
        this.Count += this.CircularBuffer.Write(data, offset, length);
        this.EndOfLastPacket = timeSpan + this.ComputeBufferDuration(data, offset, length);
        this.LastPacketSequenceNumber = sequence;
    }

    private void ReorganizeBuffer()
    {
        while(this._earlySamples.TryGetValue(this.LastPacketSequenceNumber+1, out var samplesHolder))
        {
            this.AddSamplesSimple(samplesHolder.Buffer, samplesHolder.Offset, samplesHolder.Count, samplesHolder.Sequence, samplesHolder.TimeSpan);
        }
    }
    
    public int Read(byte[] buffer, int offset, int count)
    {
        this.Lock.Wait();
        int readBytes = this.CircularBuffer.Read(buffer, offset, count);
        this.Count -= readBytes;
        if(this.Count <= 0) this.Reset();
        return readBytes;
        this.Lock.Release();
    }

    private void Reset()
    {
        foreach (var samples in this._earlySamples.ToList())
        {
            if(samples.Key < this.LastPacketSequenceNumber + 1) this._earlySamples.Remove(samples.Key);
        }
        this.IsReset = true;
    }
    
    /*public bool AddSamples(byte[] buffer, int offset, int count, TimeSpan timeSpan, ulong sequence)
    {
        this.Lock.Wait();
        if (this.IsFirstPacket)
        {
            this.LastValidTimeSpan = timeSpan;
            this.LastValidSequence = sequence;
            this.LastValidBufferDuration = ComputeBufferDuration(buffer, offset, count);
            this.CircularBuffer.Write(buffer, offset, count);
            this.CircularBuffer
            this.IsFirstPacket = false;
        }
        else if (sequence <= this.LastValidSequence)
        {
            this.Lock.Release();
            return false;
        }
        
        else if (this.LastValidSequence + 1 != sequence && !this._earlySamples.ContainsKey(sequence))
        {
            this._earlySamples.Add(sequence, new SamplesHolder(buffer, offset, count, timeSpan, sequence));
        }
        else
        {
            this.ForceAddSamples(buffer, offset, count, timeSpan, sequence);
            this.ReorganizeBufferWithEarlySamples();
        }
        this.Lock.Release();
        return true;
    }
    private void ForceAddSamples(byte[] buffer, int offset, int count, TimeSpan timeSpan, ulong sequence)
    {
        this.CircularBuffer.Advance(this.WaveFormat.ConvertLatencyToByteSize((int) (timeSpan - this.LastValidTimeSpan + this.LastValidBufferDuration).TotalMilliseconds));
        this.LastValidTimeSpan = timeSpan;
        this.LastValidSequence = sequence;
        this.LastValidBufferDuration = ComputeBufferDuration(buffer, offset, count);
        this.Counter += this.CircularBuffer.Write(buffer, offset, count);
        if (this.Counter >= this.FrameSize)
        {
            this.Counter -= this.FrameSize;
            this.CircularBuffer.Read(this._dataAvailableBuffer, 0, this.FrameSize);
            this.DataAvailable?.Invoke(this._dataAvailableBuffer);
        }
    }
    private void ForceAddSamples(SamplesHolder samplesHolder)
    {
        this.ForceAddSamples(samplesHolder.Buffer, samplesHolder.Offset, samplesHolder.Count, samplesHolder.TimeSpan, samplesHolder.Sequence);
        this._earlySamples.Remove(samplesHolder.Sequence);
    }

    private void ReorganizeBufferWithEarlySamples()
    {
        while (this._earlySamples.TryGetValue(this.LastValidSequence + 1, out var samplesHolder))
        {
            this.ForceAddSamples(samplesHolder);
        }
    }*/

    private record SamplesHolder(byte[] Buffer, int Offset, int Count, TimeSpan TimeSpan, ulong Sequence);
}