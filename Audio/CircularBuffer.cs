namespace NetLib.Audio;

public class CircularBuffer
{
  private readonly byte[] buffer;
  private readonly object lockObject;
  private int writePosition;
  private int readPosition;
  private int byteCount;

  public int WritePosition
  {
    get => this.writePosition;
    set => this.writePosition = value;
  }

  public int ReadPosition
  {
    get => this.readPosition;
    set => this.readPosition = value;
  }
  
  public int ByteCount => this.byteCount;

  public CircularBuffer(int size)
  {
    this.buffer = new byte[size];
    this.lockObject = new object();
  }

  public int Write(byte[] data, int offset, int count)
  {
    lock (this.lockObject)
    {
      int num1 = 0;
      if (count > this.buffer.Length - this.byteCount)
        count = this.buffer.Length - this.byteCount;
      int length = Math.Min(this.buffer.Length - this.writePosition, count);
      Array.Copy((Array) data, offset, (Array) this.buffer, this.writePosition, length);
      this.writePosition += length;
      this.writePosition %= this.buffer.Length;
      int num2 = num1 + length;
      if (num2 < count)
      {
        Array.Copy((Array) data, offset + num2, (Array) this.buffer, this.writePosition, count - num2);
        this.writePosition += count - num2;
        num2 = count;
      }
      this.byteCount += num2;
      return num2;
    }
  }

  public int Read(byte[] data, int offset, int count)
  {
    lock (this.lockObject)
    {
      if (count > this.byteCount)
        count = this.byteCount;
      int num1 = 0;
      int length = Math.Min(this.buffer.Length - this.readPosition, count);
      Array.Copy((Array) this.buffer, this.readPosition, (Array) data, offset, length);
      int num2 = num1 + length;
      this.readPosition += length;
      this.readPosition %= this.buffer.Length;
      if (num2 < count)
      {
        Array.Copy((Array) this.buffer, this.readPosition, (Array) data, offset + num2, count - num2);
        this.readPosition += count - num2;
        num2 = count;
      }
      this.byteCount -= num2;
      return num2;
    }
  }

  public int MaxLength => this.buffer.Length;

  public int Count
  {
    get
    {
      lock (this.lockObject)
        return this.byteCount;
    }
  }

  public void Reset()
  {
    lock (this.lockObject)
      this.ResetInner();
  }

  private void ResetInner()
  {
    this.byteCount = 0;
    this.readPosition = 0;
    this.writePosition = 0;
  }

  public void Advance(int count)
  {
    lock (this.lockObject)
    {
      if (count >= this.byteCount)
      {
        this.ResetInner();
      }
      else
      {
        this.byteCount -= count;
        this.readPosition += count;
        this.readPosition %= this.MaxLength;
      }
    }
  }
}