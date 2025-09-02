// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents the unique identifier for a persisted <see cref="ClosedStreamState"/>.
/// </summary>
/// <param name="EventSourceId">The unique identifier of the event source the stream belongs to.</param>
/// <param name="EventStreamType">The type or category of the event stream.</param>
/// <param name="EventStreamId">The identifier of the stream within the specified stream type.</param>
public record ClosedStreamId(EventSourceId EventSourceId, EventStreamType EventStreamType, EventStreamId EventStreamId);