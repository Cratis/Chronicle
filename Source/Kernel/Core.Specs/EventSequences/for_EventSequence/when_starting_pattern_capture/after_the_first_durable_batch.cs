// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_starting_pattern_capture;

public class after_the_first_durable_batch : given.appending_many_events
{
    AppendManyResult _result;

    void Establish() => _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
        .Returns(call => Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(
            AppendedEventsFrom(call.Arg<IEnumerable<EventToAppendToStorage>>()))));

    async Task Because()
    {
        _result = await _eventSequence.AppendMany(
            _events,
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_subscribe_pattern_capture() => _patternCapture.Received(1).RecoverSubscription(EventStore, EventStoreNamespace);
    [Fact] void should_keep_the_reconciliation_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
