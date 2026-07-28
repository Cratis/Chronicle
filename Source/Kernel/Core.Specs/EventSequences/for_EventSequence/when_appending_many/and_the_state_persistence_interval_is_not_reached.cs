// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_the_state_persistence_interval_is_not_reached : given.appending_many_events
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

        _silo.StorageStats<EventSequence, EventSequenceState>().ResetCounts();
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_write_state() => _silo.StorageStats<EventSequence, EventSequenceState>().Writes.ShouldEqual(0);
}
