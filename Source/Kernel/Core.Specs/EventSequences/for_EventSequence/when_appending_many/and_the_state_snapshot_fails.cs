// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

/// <summary>
/// The batch path takes the same warm-start snapshot after the batch is durable, so a failure there must leave the
/// batch reported as appended, dispatched for live delivery and indexed.
/// </summary>
public class and_the_state_snapshot_fails : given.an_event_sequence_that_cannot_persist_state
{
    AppendManyResult _result;

    void Establish() =>
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(callInfo =>
            {
                var events = callInfo.Arg<IEnumerable<EventToAppendToStorage>>();
                return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(events)));
            });

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_report_the_durable_batch_as_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_still_dispatch_the_batch_for_live_delivery() => _appendedEventsQueues.Received(1).Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_still_update_the_constraint_index_once_per_event() => _constraintIndexSequenceNumbers.Count.ShouldEqual(3);
}
