namespace NetLib.Struct;

public class BiDirectionalCircularBuffer<T> where T : struct
{
    private readonly T[] _buffer;
    private readonly int _capacity;
    private int _head;
    private int _tail;
    private int _count;

    public BiDirectionalCircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
        _capacity = capacity;
        _head = _capacity / 2;
        _tail = _head;
    }

    public int WriteHead(T[] buffer, int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_head == _tail && _count > 0)
            {
                return i;
            }

            _buffer[_head] = buffer[offset + i];
            _head = (_head + 1) % _capacity;
            _count++;
        }

        return count;
    }

    public int WriteTail(T[] buffer, int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_head == _tail && _count > 0)
            {
                return i;
            }

            _tail = (_tail - 1 + _capacity) % _capacity;
            _buffer[_tail] = buffer[offset + i];
            _count++;
        }

        return count;
    }

    public int WriteTailValue(T value, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_head == _tail && _count > 0)
            {
                return i;
            }

            _tail = (_tail - 1 + _capacity) % _capacity;
            _buffer[_tail] = value;
            _count++;
        }

        return count;
    }

    public int WriteHeadValue(T value, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_head == _tail && _count > 0)
            {
                return i;
            }

            _head = (_head + 1) % _capacity;
            _buffer[_head] = value;
            _count++;
        }

        return count;
    }


    public int Read(T[] buffer, int offset, int count)
    {
        int i = 0;
        while (_count > 0 && count > 0)
        {
            buffer[offset + i] = _buffer[_tail];
            _buffer[_tail] = default;
            _tail = (_tail + 1) % _capacity;
            i++;
            count--;
            _count--;
        }

        return i;
    }

    public int Peek(T[] buffer, int offset, int count)
    {
        int i = 0;
        while (_count > 0 && count > 0)
        {
            buffer[offset + i] = _buffer[_tail];
            _tail = (_tail + 1) % _capacity;
            i++;
            count--;
            _count--;
        }

        return i;
    }

    public int Clear(int count)
    {
        int i = 0;
        while (_count > 0 && count > 0)
        {
            _buffer[_tail] = default;
            _tail = (_tail + 1) % _capacity;
            i++;
            count--;
            _count--;
        }

        return i;
    }

    public void Clear()
    {
        _head = _capacity / 2;
        _tail = _head;
    }

    public int Count
    {
        get => _count;
    }

    public int Capacity
    {
        get { return _capacity; }
    }
}