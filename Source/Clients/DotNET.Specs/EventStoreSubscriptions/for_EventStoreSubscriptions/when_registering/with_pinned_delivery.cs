// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptions.when_registering;

public class with_pinned_delivery : given.subscriptions
{
    async Task Because() => await _subscriptions.Subscribe("s", "source", builder => builder.WithEventType<CustomerRegistered>().WithGenerationDelivery(EventGenerationDelivery.Pinned));
    [Fact] void should_send_the_declared_generation() => _request.EventTypes.Single().Generation.ShouldEqual(3u);
    [Fact] void should_send_the_delivery_policy() => _request.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Pinned);
}
