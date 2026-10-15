// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class without_any_known_event_types : given.a_builder
{
    Exception _error;
    void Establish()
    {
        _eventTypes.All.Returns(ImmutableList<EventType>.Empty);
        _builder.WithGenerationDelivery(EventGenerationDelivery.Pinned);
    }
    void Because() => _error = Catch.Exception(() => _builder.Build());
    [Fact] void should_not_create_an_unpinned_wildcard_subscription() => _error.ShouldBeOfExactType<EventStoreSubscriptionGenerationNotResolved>();
}
