using NAudio.Wave;
using NetLib.Struct;
using WaveFormat = NAudio.Wave.WaveFormat;

namespace NetLib.Handlers;

public class JitterBuffer : IWaveProvider
{
    private TimeSpan EndOfLastPacket { get; set; }
    private ulong LastPacketSequenceNumber { get; set; }
    private SamplesHolder FirstPacket { get; set; }

    private readonly IDictionary<ulong, SamplesHolder> _earlySamples = new SortedDictionary<ulong, SamplesHolder>();
    private readonly IDictionary<ulong, SamplesHolder> _lateSamples = new SortedDictionary<ulong, SamplesHolder>();
    public WaveFormat WaveFormat { get; }
    public int Latency { get; }
    private int FrameSize { get; }
    private SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);
    private bool IsReset { get; set; } = true;
    private BiDirectionalCircularBuffer<byte> Buffer { get; }
    private IDictionary<byte[], bool> CopyBuffers { get; }

    public JitterBuffer(WaveFormat waveFormat, int latency)
    {
        this.WaveFormat = waveFormat;
        this.FrameSize = this.ComputeFrameSize(latency);
        this.Buffer = new BiDirectionalCircularBuffer<byte>(this.FrameSize * 5);
        this.CopyBuffers = new Dictionary<byte[], bool>(0);
        this.Latency = latency;
    }

    private void MakeCopy(ref byte[] buffer, int offset, int count)
    {
        // Find if the CopyBuffers contains a free buffer
        var pair = this.CopyBuffers.FirstOrDefault(x => !x.Value);
        byte[] copy = pair.Key;
        
        if (pair.Equals(default(KeyValuePair<byte[], bool>)))
        {
            copy = new byte[this.FrameSize];
        }
        
        Array.Copy(buffer, offset, copy, 0, count);
        this.CopyBuffers.Add(copy, true); // true = Is used
        buffer = copy;
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
        //return TimeSpan.FromMilliseconds(this.WaveFormat.SampleRate * this.WaveFormat.Channels / 1000.0 / length);
        return TimeSpan.FromMilliseconds(1000.0d * (offset + count) / this.ComputeFrameSize(1000));
    }

    public void AddSamples(byte[] data, int offset, int length, ulong sequence, TimeSpan timeSpan)
    {
        this.Lock.Wait();
        //Console.WriteLine($"Early samples: {this._earlySamples.Count} Late samples: {this._lateSamples.Count}");
        if (this.IsReset)
        {
            this.EndOfLastPacket = timeSpan + this.ComputeBufferDuration(data, offset, length);
            this.LastPacketSequenceNumber = sequence;
            this.FirstPacket = new SamplesHolder(data, offset, length, timeSpan, sequence);
            this.Buffer.WriteHead(data, offset, length);
            this.IsReset = false;
        }
        else if (sequence == this.LastPacketSequenceNumber + 1)
        {
            this.AddSamplesSimple(data, offset, length, sequence, timeSpan);
            //Console.WriteLine($"Adding ok samples {sequence} at {timeSpan.TotalMilliseconds}ms");
            this.TryAddEarlySamples();
        }
        else if (sequence > this.LastPacketSequenceNumber + 1)
        {
            //Console.WriteLine($"Adding early samples {sequence} at {timeSpan.TotalMilliseconds}ms");
            this.RegisterEarlySamples(data, offset, length, sequence, timeSpan);
        }
        else if (sequence < this.FirstPacket.Sequence)
        {
            //Console.WriteLine($"Adding late samples {sequence} at {timeSpan.TotalMilliseconds}ms");
            this.RegisterLateSamples(data, offset, length, sequence, timeSpan);
            this.TryAddLateSamples();
        }

        //Console.WriteLine($"{this.Buffer.Count} bytes in buffer");
        this.Lock.Release();
    }
    
    private void AddSamplesSimple(byte[] data, int offset, int length, ulong sequence, TimeSpan timeSpan)
    {
        int bufferOffset = this.WaveFormat.ConvertLatencyToByteSize((int)(timeSpan - this.EndOfLastPacket).TotalMilliseconds);
        bufferOffset = bufferOffset < 0 ? 0 : bufferOffset;
        this.Buffer.WriteHeadValue(0, this.WaveFormat.ConvertLatencyToByteSize(bufferOffset));
        this.Buffer.WriteHead(data, offset, length);
        this.EndOfLastPacket = timeSpan + this.ComputeBufferDuration(data, offset, length);
        this.LastPacketSequenceNumber = sequence;
        //Console.WriteLine($"Last packet sequence number: {this.LastPacketSequenceNumber}");
    }
    
    private void RegisterEarlySamples(byte[] data, int offset, int length, ulong sequence, TimeSpan timeSpan)
    {
        if (!this._earlySamples.ContainsKey(sequence))
        {
            this.MakeCopy(ref data, offset, length);
            this._earlySamples.Add(sequence, new SamplesHolder(data.ToArray(), offset, length, timeSpan, sequence));
        }

    }
    private void RegisterLateSamples(byte[] data, int offset, int length, ulong sequence, TimeSpan timeSpan)
    {
        if (!this._lateSamples.ContainsKey(sequence))
        {
            this.MakeCopy(ref data, offset, length);
            this._lateSamples.Add(sequence, new SamplesHolder(data.ToArray(), offset, length, timeSpan, sequence));
        }
    }
    
    private void UnregisterEarlySamples(ulong sequence)
    {
        if (this._earlySamples.ContainsKey(sequence))
        {
            this.CopyBuffers.Remove(this._earlySamples[sequence].Buffer);
            this._earlySamples.Remove(sequence);
        }
    }
    
    private void UnregisterLateSamples(ulong sequence)
    {
        if (this._lateSamples.ContainsKey(sequence))
        {
            this.CopyBuffers.Remove(this._lateSamples[sequence].Buffer);
            this._lateSamples.Remove(sequence);
        }
    }

    private void TryAddEarlySamples()
    {
        while(this._earlySamples.TryGetValue(this.LastPacketSequenceNumber+1, out var samplesHolder))
        {
            this.AddSamplesSimple(samplesHolder.Buffer, samplesHolder.Offset, samplesHolder.Count, samplesHolder.Sequence, samplesHolder.TimeSpan);
            this.UnregisterEarlySamples(samplesHolder.Sequence);
        }
    }

    private void AddLateSamples(SamplesHolder samplesHolder)
    {
        int bufferOffset = this.ComputeFrameSize((int) (samplesHolder.TimeSpan - this.ComputeBufferDuration(samplesHolder.Buffer, samplesHolder.Offset, samplesHolder.Count)).TotalMilliseconds);
        this.Buffer.WriteTailValue(0, bufferOffset);
        this.Buffer.WriteTail(samplesHolder.Buffer, samplesHolder.Offset, samplesHolder.Count);
        this.FirstPacket = samplesHolder;
    }

    private void TryAddLateSamples()
    {
        while(this._lateSamples.TryGetValue(this.FirstPacket.Sequence-1, out var samplesHolder))
        {
            this.AddLateSamples(samplesHolder);
            this.UnregisterLateSamples(samplesHolder.Sequence);
        }
    }
    
    public int Read(byte[] buffer, int offset, int count)
    {
        this.Lock.Wait();
        int length = this.Buffer.Peek(buffer, offset, count);
        if(length == 0)
        {
            this.Reset();
        }
        this.Lock.Release();
        
        return length;
    }
    
    public int Clear(int readBytes)
    {
        this.Lock.Wait();

        int length = this.Buffer.Clear(readBytes);

        this.Lock.Release();
        
        return length;
    }

    private void Reset()
    {
        this._earlySamples.Clear();
        this._lateSamples.Clear();

        this.IsReset = true;
    }
    
    private sealed record SamplesHolder(byte[] Buffer, int Offset, int Count, TimeSpan TimeSpan, ulong Sequence);
}