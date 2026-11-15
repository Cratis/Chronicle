// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Writes a preimage into a caller supplied buffer, renting from the shared pool only when that buffer is too small.
/// </summary>
internal ref struct UniqueConstraintPreimageWriter
{
    Span<byte> _buffer;
    byte[]? _rented;
    int _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueConstraintPreimageWriter"/> struct.
    /// </summary>
    /// <param name="initialBuffer">The buffer to write into first, typically stack allocated.</param>
    internal UniqueConstraintPreimageWriter(Span<byte> initialBuffer)
    {
        _buffer = initialBuffer;
        _rented = null;
        _length = 0;
    }

    /// <summary>
    /// Gets the bytes written so far.
    /// </summary>
    internal readonly ReadOnlySpan<byte> Written => _buffer[.._length];

    /// <summary>
    /// Writes a single byte.
    /// </summary>
    /// <param name="value">The byte.</param>
    internal void WriteByte(byte value)
    {
        Reserve(1);
        _buffer[_length++] = value;
    }

    /// <summary>
    /// Writes raw bytes.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    internal void Write(ReadOnlySpan<byte> bytes)
    {
        Reserve(bytes.Length);
        bytes.CopyTo(_buffer[_length..]);
        _length += bytes.Length;
    }

    /// <summary>
    /// Writes a count or length as an unsigned 32-bit big-endian integer.
    /// </summary>
    /// <param name="count">The non-negative count.</param>
    internal void WriteCount(int count)
    {
        Reserve(sizeof(uint));
        BinaryPrimitives.WriteUInt32BigEndian(_buffer[_length..], checked((uint)count));
        _length += sizeof(uint);
    }

    /// <summary>
    /// Writes text as a present marker (0x01), a length and the UTF-8 bytes. The text is encoded straight into the buffer.
    /// </summary>
    /// <param name="text">The text.</param>
    internal void WriteUtf8(string text)
    {
        Reserve(1 + sizeof(uint) + Encoding.UTF8.GetMaxByteCount(text.Length));
        _buffer[_length++] = 0x01;
        var lengthPosition = _length;
        _length += sizeof(uint);
        var written = Encoding.UTF8.GetBytes(text, _buffer[_length..]);
        BinaryPrimitives.WriteUInt32BigEndian(_buffer.Slice(lengthPosition, sizeof(uint)), (uint)written);
        _length += written;
    }

    /// <summary>
    /// Returns any rented buffer to the pool.
    /// </summary>
    internal void Dispose()
    {
        if (_rented is not null)
        {
            ArrayPool<byte>.Shared.Return(_rented);
            _rented = null;
        }
    }

    void Reserve(int additional)
    {
        if (_buffer.Length - _length >= additional)
        {
            return;
        }
        var larger = ArrayPool<byte>.Shared.Rent(Math.Max(_buffer.Length * 2, _length + additional));
        _buffer[.._length].CopyTo(larger);
        if (_rented is not null)
        {
            ArrayPool<byte>.Shared.Return(_rented);
        }
        _rented = larger;
        _buffer = larger;
    }
}
