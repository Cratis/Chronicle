// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>Buffers retained references without retaining event objects in the shared pool after use.</summary>
internal struct ObserverRunBuffer
{
    AppendedEvent[]? _buffer;
    int _count;

    /// <summary>Retains an original event reference in a reusable pooled buffer.</summary>
    /// <param name="appendedEvent">The event at or above the delivery floor.</param>
    internal void Add(AppendedEvent appendedEvent)
    {
        var buffer = _buffer ??= ArrayPool<AppendedEvent>.Shared.Rent(16);
        if (_count == buffer.Length)
        {
            buffer = Grow(buffer);
            _buffer = buffer;
        }

        buffer[_count++] = appendedEvent;
    }

    /// <summary>Copies the retained prefix into the delivery array.</summary>
    /// <returns>The ordered original event references, or an empty array.</returns>
    internal readonly AppendedEvent[] ToArray()
    {
        if (_buffer is not { } buffer)
        {
            return [];
        }

        var result = new AppendedEvent[_count];
        Array.Copy(buffer, result, _count);
        return result;
    }

    /// <summary>Clears retained references before returning the buffer to its pool.</summary>
    internal void Dispose()
    {
        if (_buffer is not { } buffer)
        {
            return;
        }

        Array.Clear(buffer, 0, _count);
        ArrayPool<AppendedEvent>.Shared.Return(buffer);
        _buffer = null;
        _count = 0;
    }

    /// <summary>Copies into a new rental without mutating the owner's fields; Add installs the returned array.</summary>
    /// <param name="buffer">The owner's current rental, returned here after its used references are cleared.</param>
    /// <returns>The new rental whose ownership is transferred to the calling Add method.</returns>
    readonly AppendedEvent[] Grow(AppendedEvent[] buffer)
    {
        var larger = ArrayPool<AppendedEvent>.Shared.Rent(checked(buffer.Length * 2));
        Array.Copy(buffer, larger, _count);
        Array.Clear(buffer, 0, _count);
        ArrayPool<AppendedEvent>.Shared.Return(buffer);
        return larger;
    }
}
