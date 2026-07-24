// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_deactivating;

public class and_state_was_accumulated_since_the_last_write : given.an_event_sequence
{
    async Task Establish()
    {
        await _eventSequence.Append(
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

        _silo.StorageStats<EventSequence, EventSequenceState>().ResetCounts();
    }

    Task Because() => _eventSequence.OnDeactivateAsync(
        new DeactivationReason(DeactivationReasonCode.None, string.Empty),
        CancellationToken.None);

    [Fact] void should_write_state_on_deactivation() => _silo.StorageStats<EventSequence, EventSequenceState>().Writes.ShouldEqual(1);
}
