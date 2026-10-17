// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_the_key_index_cannot_be_read_while_a_partition_has_failed : given.a_routing_state_with_a_failed_partition
{
    void Establish() =>
        _storage.GetEventStore(Arg.Any<Concepts.EventStoreName>()).GetNamespace(Arg.Any<Concepts.EventStoreNamespaceName>()).ObserverKeyIndexes
            .GetFor(Arg.Any<ObserverKey>(), Arg.Any<ObserverReaderScope>())
            .Returns<IObserverKeyIndex>(_ => throw new InvalidOperationException("Index unavailable"));

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_assume_catch_up_has_work() => _observer.Received(1).CatchUp();
}
