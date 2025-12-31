using System.Buffers;

namespace Yamlify.Core;

/// <summary>
/// A buffer writer that wraps a stream.
/// </summary>
internal sealed class StreamBufferWriter : IBufferWriter<byte>, IDisposable
{
    private readonly Stream _stream;
    private byte[] _buffer;
    private int _index;

    public StreamBufferWriter(Stream stream, int initialCapacity = 4096)
    {
        _stream = stream;
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    public void Advance(int count)
    {
        _index += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_index);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_index);
    }

    public void Flush()
    {
        if (_index > 0)
        {
            _stream.Write(_buffer, 0, _index);
            _index = 0;
        }
    }

    public void Dispose()
    {
        Flush();
        ArrayPool<byte>.Shared.Return(_buffer);
    }

    private void EnsureCapacity(int sizeHint)
    {
        if (sizeHint < 1) sizeHint = 1;
        
        if (_index + sizeHint > _buffer.Length)
        {
            Flush();
            
            if (sizeHint > _buffer.Length)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = ArrayPool<byte>.Shared.Rent(sizeHint);
            }
        }
    }
}
