// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming;
#pragma warning disable CA1051 // Do not declare visible instance fields

/// <summary>
/// Represents a cached appended event.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CachedAppendedEvent"/> class.
/// </remarks>
/// <param name="event">The <see cref="AppendedEvent"/> to cache.</param>
/// <param name="next">The next <see cref="CachedAppendedEvent"/> in the chain.</param>
public class CachedAppendedEvent(AppendedEvent @event, CachedAppendedEvent? next = null)
{
    /// <summary>
    /// Gets the <see cref="AppendedEvent"/> that is cached.
    /// </summary>
    public AppendedEvent Event { get; } = @event;

    /// <summary>
    /// Gets the next <see cref="CachedAppendedEvent"/> in the chain.
    /// </summary>
    public CachedAppendedEvent? Next { get; set; } = next;
}
