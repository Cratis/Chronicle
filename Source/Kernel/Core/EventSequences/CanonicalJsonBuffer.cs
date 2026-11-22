// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Owns a call-local pooled destination for canonical JSON and its optional hash prefix.
/// </summary>
internal sealed class CanonicalJsonBuffer : IBufferWriter<byte>, IDisposable
{
    byte[] _buffer = ArrayPool<byte>.Shared.Rent(1024);
    int _written;

    /// <summary>
    /// Gets the bytes written during this call.
    /// </summary>
    internal ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

    /// <inheritdoc/>
    public void Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, _buffer.Length - _written);
        _written += count;
    }

    /// <inheritdoc/>
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_written);
    }

    /// <inheritdoc/>
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_written);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_buffer.Length == 0) return;
        ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
        _buffer = [];
        _written = 0;
    }

    void EnsureCapacity(int sizeHint)
    {
        ObjectDisposedException.ThrowIf(_buffer.Length == 0, this);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
        var required = Math.Max(sizeHint, 1);
        if (required <= _buffer.Length - _written) return;

        var replacement = ArrayPool<byte>.Shared.Rent(checked(_written + Math.Max(required, _buffer.Length)));
        WrittenSpan.CopyTo(replacement);
        ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
        _buffer = replacement;
    }
}
