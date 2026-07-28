// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

/// <summary>
/// The batch path has the same post-durability ordering as the single append, so a failed hand-over to the queues
/// must leave the batch reported as appended, every constraint index updated, and the queues spilled to catch-up.
/// </summary>
public class and_the_live_dispatch_fails : given.appending_many_events
{
    AppendManyResult _result;

    void Establish()
    {
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(callInfo =>
            {
                var events = callInfo.Arg<IEnumerable<EventToAppendToStorage>>();
                return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(events)));
            });

        _appendedEventsQueues
            .Enqueue(Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(_ => Task.FromException(new given.SimulatedEnqueueError()));
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_report_the_durable_batch_as_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_still_update_the_constraint_index_once_per_event() => _constraintIndexSequenceNumbers.Count.ShouldEqual(3);
    [Fact] void should_spill_the_queues_to_catch_up() => _appendedEventsQueues.Received(1).SpillToCatchup();
}
