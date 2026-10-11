// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents an event returned by a reactor together with the destination it would be appended to.
/// </summary>
/// <param name="Event">The produced event, with any cross-stream wrapper unwrapped.</param>
/// <param name="EventSourceId">The destination event source identifier.</param>
/// <param name="EventSourceType">The destination event source type.</param>
/// <param name="EventStreamType">The destination event stream type.</param>
/// <param name="EventStreamId">The destination event stream identifier.</param>
public record ProducedEvent(
    object Event,
    EventSourceId EventSourceId,
    EventSourceType EventSourceType,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId);
