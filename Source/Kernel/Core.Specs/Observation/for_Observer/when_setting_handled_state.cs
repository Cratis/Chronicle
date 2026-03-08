// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
namespace Cratis.Chronicle.Observation.for_Observer;

public class when_setting_handled_state : given.an_observer
{
    static Exception _error;
    static EventSequenceNumber _eventSequenceNumber;

    static ObserverState _initialState;

    void Establish()
    {
        _eventSequenceNumber = 5;
        _initialState = _stateStorage.State;
    }

    async Task Because() => _error = await Catch.Exception(() => _observer.SetHandledStats(_eventSequenceNumber));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact]
    void should_have_correct_state() => _stateStorage.State.ShouldEqual(_initialState with
    {
        LastHandledEventSequenceNumber = _eventSequenceNumber
    });
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
}
