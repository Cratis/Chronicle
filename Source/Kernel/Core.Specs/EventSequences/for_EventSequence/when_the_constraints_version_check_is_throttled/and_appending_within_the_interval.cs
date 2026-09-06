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
    /// Activation reads the version, but does not count as a completed check - the first append is what starts the
    /// throttle. Only the appends are of interest here.
    /// </summary>
    void Establish() => _constraintsGrain.ClearReceivedCalls();

    async Task Because()
    {
        await Append();
        await Append();
        await Append();
    }

    [Fact] void should_check_the_constraints_version_on_the_first_append() =>
        _constraintsGrain.Received(1).GetVersion();

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
