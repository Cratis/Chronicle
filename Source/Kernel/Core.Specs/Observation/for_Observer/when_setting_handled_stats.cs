// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_setting_handled_stats : given.an_observer
{
    EventSequenceNumber _sequenceNumber;

    void Establish() => _sequenceNumber = 42UL;
    Task Because() => _observer.SetHandledStats(_sequenceNumber);

    [Fact] void should_set_correct_last_handled_event_sequence_number() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_sequenceNumber);
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
}
