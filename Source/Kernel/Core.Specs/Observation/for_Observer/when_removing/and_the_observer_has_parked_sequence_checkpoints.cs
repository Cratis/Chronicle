// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_removing;

public class and_the_observer_has_parked_sequence_checkpoints : given.an_absent_observer
{
    async Task Establish() => await Crash();

    async Task Because() => await _observer.Remove();

    [Fact] void should_remove_the_parked_checkpoints() => _eventStoreNamespaceStorage.ObserverSequenceCheckpoints.Received(1).RemoveAllFor(_observerId);
}
