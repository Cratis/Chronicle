// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_setting_handled_state : given.an_observer
{
    static Exception error;
    static EventCount eventCount;
    static EventSequenceNumber eventSequenceNumber;

    static ObserverState initialState;

    void Establish()
    {
        eventCount = 4;
        eventSequenceNumber = 5;
        initialState = state_storage.State;
    }

    async Task Because() => error = await Catch.Exception(() => observer.SetHandledStats(eventCount, eventSequenceNumber));

    [Fact] void should_not_fail() => error.ShouldBeNull();
    [Fact] void should_have_correct_state() => state_storage.State.ShouldEqual(initialState with
    {
        Handled = eventCount,
        LastHandledEventSequenceNumber = eventSequenceNumber
    });
    [Fact] void should_write_state_once() => storage_stats.Writes.ShouldEqual(1);
}