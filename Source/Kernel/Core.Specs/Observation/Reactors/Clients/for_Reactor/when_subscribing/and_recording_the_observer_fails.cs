// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class and_recording_the_observer_fails : given.a_reactor_grain_with_a_stored_definition
{
    Exception _exception;

    void Establish() => _connectedClients.RecordObserver(Arg.Any<Cratis.Chronicle.Concepts.Clients.ConnectionId>(), Arg.Any<Cratis.Chronicle.Concepts.Observation.ObserverKey>()).Returns(Task.FromException(new InvalidOperationException("Unsupported in a rolling upgrade")));

    async Task Because() => _exception = await Catch.Exception(() => _grain.SetDefinitionAndSubscribe(_connectionDefinition));

    [Fact] void should_not_fail_the_subscription() => _exception.ShouldBeNull();
    [Fact] Task should_still_subscribe() => WasSubscribed(_connectionDefinition);
}
