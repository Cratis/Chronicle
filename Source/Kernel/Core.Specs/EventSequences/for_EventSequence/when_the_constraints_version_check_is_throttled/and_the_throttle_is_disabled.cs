// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_the_constraints_version_check_is_throttled;

/// <summary>
/// The counterpart to <see cref="and_appending_within_the_interval"/>: it is the throttle that suppresses the
/// per-append check, not the absence of one. Without this, an interval that accidentally never checked at all
/// would satisfy that spec just as well.
/// </summary>
public class and_the_throttle_is_disabled : given.an_event_sequence
{
    protected override TimeSpan ConstraintsVersionCheckInterval => TimeSpan.Zero;

    void Establish() => _constraintsGrain.ClearReceivedCalls();

    async Task Because()
    {
        await Append();
        await Append();
    }

    [Fact] void should_check_the_constraints_version_on_every_append() =>
        _constraintsGrain.Received(2).GetVersion();

    Task Append() => _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        new JsonObject(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);
}
