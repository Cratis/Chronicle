// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Represents an event stream declared by an <see cref="IEventSource"/> definition.
/// </summary>
/// <param name="Name">The name of the stream, which is the event stream type of appended events.</param>
/// <param name="Description">The description of the stream.</param>
/// <param name="Concurrency">The dimensions that take part in concurrency checks for the stream.</param>
public record EventStream(string Name, string Description, ConcurrencyDimensions Concurrency)
{
    /// <summary>
    /// Gets the name as an <see cref="Events.EventStreamType"/>.
    /// </summary>
    public Events.EventStreamType EventStreamType => new(Name);
}
