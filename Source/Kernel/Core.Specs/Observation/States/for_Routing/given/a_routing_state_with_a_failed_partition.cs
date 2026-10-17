// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Observation.States.for_Routing.given;

public class a_routing_state_with_a_failed_partition : a_routing_state
{
    protected Key _failedPartition;
    protected Key _healthyPartition;
    protected IEnumerable<Key> _partitionsWithEventsBehind;

    void Establish()
    {
        _failedPartition = new Key("failed", ArrayIndexers.NoIndexers);
        _healthyPartition = new Key("healthy", ArrayIndexers.NoIndexers);
        _partitionsWithEventsBehind = [_failedPartition];

        _storedState = _storedState with { NextEventSequenceNumber = 5UL };
        _tailEventSequenceNumbers = _tailEventSequenceNumbers with { Tail = 20UL, TailForEventTypes = 20UL };

        _observer.HasFailedPartitions().Returns(true);
        _observer.GetFailedPartitionKeys().Returns(_ => Task.FromResult<IEnumerable<Key>>([_failedPartition]));

        var index = Substitute.For<IObserverKeyIndex>();
        index.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(_ => new ObserverKeysForTesting(_partitionsWithEventsBehind));
        var indexes = Substitute.For<IObserverKeyIndexes>();
        indexes.GetFor(Arg.Any<ObserverKey>(), Arg.Any<ObserverReaderScope>()).Returns(index);
        _storage.GetEventStore(Arg.Any<Concepts.EventStoreName>()).GetNamespace(Arg.Any<Concepts.EventStoreNamespaceName>()).ObserverKeyIndexes.Returns(indexes);
    }
}
