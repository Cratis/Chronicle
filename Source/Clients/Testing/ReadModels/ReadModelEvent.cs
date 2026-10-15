// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents a seeded event together with its source identity and stream routing.
/// </summary>
/// <param name="EventSourceId">The event source the event belongs to.</param>
/// <param name="Event">The seeded event.</param>
/// <param name="EventSourceType">The type of event source.</param>
/// <param name="EventStreamType">The type of event stream.</param>
/// <param name="EventStreamId">The event stream identifier.</param>
/// <param name="Tags">The effective static and append tags captured when seeded.</param>
internal record ReadModelEvent(
    EventSourceId EventSourceId,
    object Event,
    EventSourceType EventSourceType,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    IReadOnlyList<string> Tags);
