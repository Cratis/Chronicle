// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class with_an_ambiguous_identifier : given.a_builder
{
    Exception _error;
    void Establish()
    {
        _eventTypes.All.Returns(ImmutableList.Create(new EventType("customer", 1), new EventType("customer", 3)));
        _builder.WithEventType("customer").WithGenerationDelivery(EventGenerationDelivery.Pinned);
    }
    void Because() => _error = Catch.Exception(() => _builder.Build());
    [Fact] void should_require_one_declared_generation() => _error.ShouldBeOfExactType<MultipleGenerationsOfEventTypeInObserver>();
}
