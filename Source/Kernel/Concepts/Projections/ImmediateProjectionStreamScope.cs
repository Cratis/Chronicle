// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Projections;

/// <summary>
/// Identifies the event stream folded by an immediate projection.
/// </summary>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventSourceType">The optional event source type.</param>
/// <param name="EventStreamType">The event stream type.</param>
/// <param name="EventStreamId">The event stream identifier.</param>
public record ImmediateProjectionStreamScope(
    EventSourceId EventSourceId,
    EventSourceType? EventSourceType,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId);
