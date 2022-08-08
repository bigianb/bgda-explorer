using System.Text;

namespace JetBlackEngineLib.Data;

public class DataReader
{
    private readonly int _baseOffset;
    private readonly byte[] _data;
    private int _offset;

    public int RealOffset
    {
        get => _baseOffset + _offset;
        set => SetOffset(value - _baseOffset);
    }

    public int Offset
    {
        get => _offset;
        set => SetOffset(value);
    }

    public int Length { get; }

    public DataReader(byte[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _baseOffset = 0;
        Length = _data.Length;
    }

    public DataReader(byte[] data, int baseOffset, int length)
    {
        _data = data;
        _baseOffset = baseOffset;
        Length = length;

        _offset = 0;
    }

    public void SetOffset(int offset)
    {
        if (offset < 0 || offset > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        _offset = offset;
    }

    public void Skip(int bytesToSkip)
    {
        if (_offset + bytesToSkip < 0 || _offset + bytesToSkip > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(bytesToSkip));
        }

        _offset = _offset + bytesToSkip;
    }

    public void Rewind(int bytesToRewind)
    {
        Skip(-bytesToRewind);
    }

    public byte ReadByte()
    {
        if (_offset + 1 >= Length)
        {
            throw new IndexOutOfRangeException("No more data");
        }

        return _data[_baseOffset + _offset++];
    }

    public byte[] ReadBytes(int count)
    {
        if (_offset + count > Length)
        {
            throw new IndexOutOfRangeException("No more data");
        }

        var value = new byte[count];

        Array.Copy(_data, _baseOffset + _offset, value, 0, count);

        _offset += count;

        return value;
    }

    public int ReadInt32()
    {
        var value = BitConverter.ToInt32(ReadBytes(4), 0);
        return value;
    }

    public short ReadInt16()
    {
        var value = BitConverter.ToInt16(ReadBytes(2), 0);
        return value;
    }

    public float ReadFloat()
    {
        var value = BitConverter.ToSingle(ReadBytes(4), 0);
        return value;
    }

    public double ReadDouble()
    {
        var value = BitConverter.ToDouble(ReadBytes(8), 0);
        return value;
    }

    public string ReadString(int length)
    {
        if (_offset + length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var value = Encoding.ASCII.GetString(_data, _baseOffset + _offset, length);
        _offset += length;
        return value;
    }

    public string ReadZString()
    {
        var stringLength = 0;
        for (var i = _offset; i < Length; i++)
        {
            if (_data[_baseOffset + i] == 0)
            {
                break;
            }

            stringLength++;
        }

        var value = ReadString(stringLength);
        if (_offset != Length)
        {
            Skip(1); // Skip the zero
        }

        return value;
    }
}