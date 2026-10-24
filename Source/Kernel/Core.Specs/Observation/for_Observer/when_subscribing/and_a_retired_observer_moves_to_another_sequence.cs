// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_retired_observer_moves_to_another_sequence : given.a_retired_observer_on_another_sequence_with_guarded_storage
{
    Task Because() => _observer.Subscribe<ObserverSubscriber>(ObserverType.Projection, [], SiloAddress.Zero);

    [Fact] void should_persist_the_definition_on_the_new_sequence() => _storedDefinition.EventSequenceId.ShouldEqual(_observerKey.EventSequenceId);
    [Fact] void should_discard_the_stored_failed_partitions() => _storedFailures.Partitions.ShouldBeEmpty();
    [Fact] void should_write_the_cleanup_through_the_guarded_provider() => _eventStoreNamespaceStorage.FailedPartitions.Received(1).Save(_observerId, Arg.Is<FailedPartitions>(failures => !failures.Partitions.Any()));
    [Fact] void should_reactivate_the_observer() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Active);
}
