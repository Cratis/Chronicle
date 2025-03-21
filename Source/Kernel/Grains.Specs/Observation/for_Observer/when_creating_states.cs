// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using Cratis.Applications.Orleans.StateMachines;
using Cratis.Chronicle.Grains.Observation.States;
using Cratis.Chronicle.Storage.Observation;
namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_creating_states : given.an_observer
{
    IImmutableList<IState<ObserverState>> states;

    void Because() => states = _observer.CreateStates();

    [Fact] void should_return_4_states() => states.Count.ShouldEqual(4);
    [Fact] void should_return_disconnected_state() => states.FirstOrDefault(s => s is Disconnected).ShouldNotBeNull();
    [Fact] void should_return_routing_state() => states.FirstOrDefault(s => s is Routing).ShouldNotBeNull();
    [Fact] void should_return_replay_state() => states.FirstOrDefault(s => s is Replay).ShouldNotBeNull();
    [Fact] void should_return_observing_state() => states.FirstOrDefault(s => s is Observing).ShouldNotBeNull();
}
