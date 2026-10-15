// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_IEventStoreSubscriptionBuilder.when_selecting_delivery;

public class with_pinned_delivery : given.a_legacy_builder
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => _builder.WithGenerationDelivery(EventGenerationDelivery.Pinned));
    [Fact] void should_fail_instead_of_downgrading() => _error.ShouldBeOfExactType<EventStoreSubscriptionGenerationDeliveryNotSupported>();
}
