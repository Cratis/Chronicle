// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_addressed_under_a_mismatching_key;

/// <summary>
/// Registering an observer in a namespace where it has no state yet, while the shared definition is still on another
/// event sequence. Starting at the tail is part of that registration, so its position must be persisted.
/// </summary>
public class and_starting_at_tail_as_a_new_observer : given.an_observer_under_a_mismatching_key
{
    bool _result;

    void Establish()
    {
        _persistedStateStorage!.PersistedState = ObserverState.Empty;
        _eventSequence.GetTailSequenceNumber().Returns(Task.FromResult<EventSequenceNumber>(100UL));
    }

    async Task Because() => _result = await _observer.StartAtTailWhenNew();

    [Fact] void should_report_that_it_was_positioned() => _result.ShouldBeTrue();
    [Fact] void should_persist_the_event_after_the_tail() => _persistedStateStorage!.PersistedState.NextEventSequenceNumber.ShouldEqual<EventSequenceNumber>(101UL);
    [Fact] void should_persist_the_identifier() => _persistedStateStorage!.PersistedState.Identifier.ShouldEqual(_observerId);
}
