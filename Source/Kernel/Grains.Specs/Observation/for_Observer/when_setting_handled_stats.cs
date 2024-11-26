// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Cratis.Chronicle.Concepts.Events;
namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_setting_handled_stats : given.an_observer
{
    EventSequenceNumber sequenceNumber;

    void Establish() => sequenceNumber = 42UL;
    Task Because() => observer.SetHandledStats(sequenceNumber);

    [Fact] void should_set_correct_last_handled_event_sequence_number() => state_storage.State.LastHandledEventSequenceNumber.ShouldEqual(sequenceNumber);
    [Fact] void should_write_state_once() => storage_stats.Writes.ShouldEqual(1);
}