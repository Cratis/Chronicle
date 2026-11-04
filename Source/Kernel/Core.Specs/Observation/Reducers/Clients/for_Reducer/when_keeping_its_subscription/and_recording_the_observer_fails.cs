// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_keeping_its_subscription;

public class and_recording_the_observer_fails : given.a_reducer_grain_with_a_stored_definition
{
    Exception _exception;

    void Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(false);
        _connectedClients.RecordObserver(Arg.Any<Cratis.Chronicle.Concepts.Clients.ConnectionId>(), Arg.Any<Cratis.Chronicle.Concepts.Observation.ObserverKey>()).Returns(Task.FromException(new InvalidOperationException("Unsupported in a rolling upgrade")));
    }

    async Task Because() => _exception = await Catch.Exception(() => _grain.KeepSubscription(_connectionDefinition));

    [Fact] void should_not_fail_keeping_the_subscription() => _exception.ShouldBeNull();
    [Fact] Task should_still_restore_the_subscription() => WasRestored(1);
}
