// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_setting_handled_state : given.an_observer
{
    static Exception error;
    static EventSequenceNumber eventSequenceNumber;

    static ObserverState initialState;

    void Establish()
    {
        eventSequenceNumber = 5;
        initialState = _stateStorage.State;
    }

    async Task Because() => error = await Catch.Exception(() => _observer.SetHandledStats(eventSequenceNumber));

    [Fact] void should_not_fail() => error.ShouldBeNull();
    [Fact] void should_have_correct_state() => _stateStorage.State.ShouldEqual(initialState with
    {
        LastHandledEventSequenceNumber = eventSequenceNumber
    });
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
}