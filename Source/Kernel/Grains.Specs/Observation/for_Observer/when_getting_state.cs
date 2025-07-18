// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_getting_state : given.an_observer
{
    ObserverState _state;

    async Task Because() => _state = await _observer.GetState();

    [Fact] void should_return_same_state() => _stateStorage.State.ShouldEqual(_state);
}
