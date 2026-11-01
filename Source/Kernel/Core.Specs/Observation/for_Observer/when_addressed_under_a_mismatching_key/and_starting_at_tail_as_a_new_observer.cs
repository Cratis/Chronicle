// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_addressed_under_a_mismatching_key;

/// <summary>
/// Requesting a tail position without a subscription cannot claim a shared definition on another event sequence.
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

    [Fact] void should_report_that_it_was_not_positioned() => _result.ShouldBeFalse();
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_write_the_definition() => _definitionStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_create_the_observer() => _persistedStateStorage!.PersistedState.Identifier.ShouldEqual(ObserverState.Empty.Identifier);
    [Fact] void should_keep_the_definition_on_its_event_sequence() => _definitionStorage.State.EventSequenceId.ShouldEqual(_definitionEventSequenceId);
    [Fact] void should_not_register_a_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
}
