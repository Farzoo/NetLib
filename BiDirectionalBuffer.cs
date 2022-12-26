

namespace NetLib;

public class BiDirectionalBuffer
{
    private readonly object _afterPositionLock = new object();
    private readonly object _beforePositionLock = new object();
    private readonly byte[] _buffer;
    private int _origin;
    private int _position;
    private int _inOrderPosition;
    private bool _isReset;
    
    public bool ThrowOnFull { get; }

    public int Origin => _origin;

    public int Position => _position;

    public bool IsEmpty => _position == _origin;
    
    public int Length => _buffer.Length;
    
    public int Count => _position - _origin;
    public BiDirectionalBuffer(int size, bool throwOnFull = false)
    {
        this._buffer = new byte[size];
        this.ThrowOnFull = throwOnFull;
        this.Reset();
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        lock (_beforePositionLock)
        {
            int length;
            lock (_afterPositionLock) length = Math.Min(count, _position - _origin);
            if(length > 0)
            {
                Array.Copy(_buffer, _origin, buffer, offset, length);
                /*Array.Reverse(buffer, offset, _inOrderPosition - _origin);
                _inOrderPosition = _origin + length > _inOrderPosition ? _origin + length : _inOrderPosition;*/
                Array.Clear(_buffer, _origin, length);
                _origin += length;
            }
            else if(!_isReset)
            {
                this.Reset();
            }
            return length;
        }
    }
    
    public int Peek(byte[] buffer, int offset, int count)
    {
        lock (_beforePositionLock)
        {
            int length;
            lock(_afterPositionLock) length = Math.Min(count, _position - _origin);
            if(length > 0)
            {
                Array.Copy(_buffer, _origin, buffer, offset, length);
            }
            else if(!_isReset)
            {
                this.Reset();
                Console.WriteLine("Reset");
            }
            return length;
        }
    }

    public int Clear(int count)
    {
        lock (_beforePositionLock)
        {
            int length;
            lock(_afterPositionLock) length = Math.Min(count, _position - _origin);
            if(length > 0)
            {
                Array.Clear(_buffer, _origin, length);
                _origin += length;
            } else if(!_isReset)
            {
                this.Reset();
            }
            return length;
        }
    }

    public int Write(byte[] buffer, int offset, int count, Direction direction)
    {
        int length;
        switch (direction)
        {
            case Direction.Forward: default:
                lock(_afterPositionLock)
                {
                    length = Math.Min(count, _buffer.Length - _position);
                    Array.Copy(buffer, offset, _buffer, _position, length);
                    // Count number of zeros
                    int zeros = 0;
                    for(int i = _position; i < _position + length; i++)
                    {
                        if(_buffer[i] == 0)
                        {
                            zeros++;
                        }
                    }
                    
                    Console.WriteLine($"Number of zeros written: {zeros} in {length} bytes");
                    _position += length;
                }
                break;
            case Direction.Backward:
                lock(_beforePositionLock)
                {
                    length = Math.Min(count, _origin);
                    /*
                    Array.Reverse(buffer, offset, count);
                    Array.Copy(buffer, offset, _buffer, _origin - count, length);
                    */
                    /*for (int i = 0; i < length; i++)
                        _buffer[_origin - i - 1] = buffer[offset + i];*/
                    for (int i = 0; i < length; i++)
                    {
                        _buffer[_origin - i - 1] = buffer[offset + length - i - 1];
                    }
                    _origin -= length;
                } 
                break;
        }

        if (length == 0)
        {
            if (this.ThrowOnFull)
                throw new InternalBufferOverflowException();
        } else if(this._isReset) 
            this._isReset = false;

        return length;
    }

    public int Write(byte value, int count, Direction direction)
    {
        int length;
        switch (direction)
        {
            case Direction.Forward: default:
                lock(_afterPositionLock)
                {
                    length = Math.Min(count, _buffer.Length - _position);
                    for (int i = 0; i < length; i++)
                        _buffer[_position + i] = value;
                    _origin += length;
                }
                break;
            case Direction.Backward:
                lock(_beforePositionLock)
                {
                    length = Math.Min(count, _origin);
                    for (int i = 0; i < length; i++)
                        _buffer[_origin - i - 1] = value;
                    _origin -= length;
                }
                break;
        }
        if (length == 0)
        {
            if (this.ThrowOnFull)
                throw new InternalBufferOverflowException();
        } else if(this._isReset) 
            this._isReset = false;

        
        return length;
    }

    private void Reset()
    {
        _position = _inOrderPosition =_origin = _buffer.Length / 2;
        _isReset = true;
    }
    
    public enum Direction
    {
        Forward,
        Backward
    }
}