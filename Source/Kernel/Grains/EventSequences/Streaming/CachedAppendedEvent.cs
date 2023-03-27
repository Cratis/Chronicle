// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;
#pragma warning disable CA1051 // Do not declare visible instance fields

/// <summary>
/// Represents a cached appended event.
/// </summary>
public class CachedAppendedEvent
{
    /// <summary>
    /// Gets the <see cref="AppendedEvent"/> that is cached.
    /// </summary>
    public AppendedEvent Event { get; }

    /// <summary>
    /// Gets the next <see cref="CachedAppendedEvent"/> in the chain.
    /// </summary>
    public CachedAppendedEvent? Next { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedAppendedEvent"/> class.
    /// </summary>
    /// <param name="event">The <see cref="AppendedEvent"/> to cache.</param>
    /// <param name="next">The next <see cref="CachedAppendedEvent"/> in the chain.</param>
    public CachedAppendedEvent(AppendedEvent @event, CachedAppendedEvent? next = null)
    {
        Event = @event;
        Next = next;
    }
}
