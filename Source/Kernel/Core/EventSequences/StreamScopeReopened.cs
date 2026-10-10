// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.EventSequences;

/// <summary>
/// Records an operator's repair of an exact manual closure in the System sequence.
/// The actor and time are carried in the event context.
/// </summary>
/// <param name="Sequence">The repaired sequence.</param>
/// <param name="EventSourceId">The participating event source identifier, or null.</param>
/// <param name="EventSourceType">The participating event source type, or null.</param>
/// <param name="EventStreamType">The participating stream type, or null.</param>
/// <param name="EventStreamId">The participating stream identifier, or null.</param>
/// <param name="Reason">Why the operator reopened the scope.</param>
[EventType, AllEventStores]
public record StreamScopeReopened(
    EventSequenceId Sequence,
    string? EventSourceId,
    string? EventSourceType,
    string? EventStreamType,
    string? EventStreamId,
    string Reason);
