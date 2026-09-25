// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_there_are_in_flight_events : given.all_dependencies
{
    static readonly Key _partition = "some-partition";

    void Establish() =>
        _firstNamespaceStorage.InFlightEvents.GetFor(_observerId).Returns(
        [
            new InFlightEvent { ObserverId = _observerId, Partition = _partition, EventSequenceNumber = 42 }
        ]);

    async Task Because() => await Remove();

    [Fact] async Task should_remove_the_in_flight_entry() =>
        await _firstNamespaceStorage.InFlightEvents.Received(1).Remove(_observerId, _partition, 42);
}
