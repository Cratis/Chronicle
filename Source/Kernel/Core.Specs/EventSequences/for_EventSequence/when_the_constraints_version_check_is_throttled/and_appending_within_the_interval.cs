// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_the_constraints_version_check_is_throttled;

public class and_appending_within_the_interval : given.an_event_sequence
{
    protected override TimeSpan ConstraintsVersionCheckInterval => TimeSpan.FromHours(1);

    /// <summary>
    /// Activation reads the version, which is what the throttle then measures from; only appends are of interest.
    /// </summary>
    void Establish() => _constraintsGrain.ClearReceivedCalls();

    async Task Because()
    {
        await Append();
        await Append();
    }

    [Fact] void should_not_check_the_constraints_version_again() =>
        _constraintsGrain.DidNotReceive().GetVersion();

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
