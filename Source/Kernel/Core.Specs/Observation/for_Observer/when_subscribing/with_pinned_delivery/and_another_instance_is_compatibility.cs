// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing.with_pinned_delivery;

public class and_another_instance_is_compatibility : given.an_observer
{
    readonly EventType _pin = new("person-registered", 2);
    readonly ConnectedClient _first = new() { ConnectionId = "first", Version = "1.0.0" };
    readonly ConnectedClient _second = new() { ConnectionId = "second", Version = "1.0.0" };
    async Task Establish() => await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_pin], SiloAddress.Zero, _first);
    async Task Because()
    {
        _eventTypesStorage.HasFor(_pin.Id, _pin.Generation).Returns(true);
        await _observer.SubscribeWithGenerationDelivery<NullObserverSubscriber>(ObserverType.Reactor, [_pin], SiloAddress.Zero, EventGenerationDelivery.Pinned, _second);
    }
    [Fact] async Task should_not_fan_out_across_policies() => (await _observer.GetSubscription()).Targets.Select(_ => _.ConnectedClient!.ConnectionId).ShouldContainOnly(_second.ConnectionId);
}
