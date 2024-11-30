// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Chronicle.Concepts.Events;
namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_setting_handled_stats : given.an_observer
{
    EventSequenceNumber sequenceNumber;

    void Establish() => sequenceNumber = 42UL;
    Task Because() => _observer.SetHandledStats(sequenceNumber);

    [Fact] void should_set_correct_last_handled_event_sequence_number() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(sequenceNumber);
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
}