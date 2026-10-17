// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_reconciling_subscription;

public class and_the_activation_is_sealed : given.a_sealed_observer
{
    Task Because() => _observer.ReconcileSubscription<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero, false);

    [Fact] async Task should_not_subscribe() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_read_schemas() => _eventTypesStorage.ReceivedCalls().ShouldBeEmpty();
}
