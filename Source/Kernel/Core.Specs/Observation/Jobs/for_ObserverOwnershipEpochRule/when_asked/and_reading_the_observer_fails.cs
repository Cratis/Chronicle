// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOwnershipEpochRule.when_asked;

public class and_reading_the_observer_fails : given.a_rule
{
    Exception _error;

    void Establish() => _storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Observers.Get(_observerKey.ObserverId)
        .Returns<Task<Storage.Observation.ObserverState>>(_ => throw new InvalidOperationException());

    async Task Because() => _error = await Catch.Exception(() => Ask(PartitionCatchUp(7)));

    [Fact] void should_let_the_failure_propagate() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
