// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle;

/// <summary>
/// Represents the context an <see cref="IEventTagEnricher"/> gets when asked for the named tags of an event about to be appended.
/// </summary>
/// <param name="EventStoreName">The name of the event store the event is appended to.</param>
/// <param name="EventSequenceId">The event sequence the event is appended to.</param>
/// <param name="EventSourceId">The event source the event is appended for.</param>
/// <param name="EventType">The <see cref="EventType"/> of the event.</param>
/// <param name="Event">The event instance.</param>
/// <param name="ExistingTags">The named tags the event already has at this point (for example supplied at the call site).</param>
public record EventTagContext(
    EventStoreName EventStoreName,
    EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    EventType EventType,
    object Event,
    IReadOnlyList<NamedTag> ExistingTags);
