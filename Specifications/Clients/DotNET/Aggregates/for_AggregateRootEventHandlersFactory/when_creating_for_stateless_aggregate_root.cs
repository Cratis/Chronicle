// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootEventHandlersFactory;

public class when_creating_for_stateless_aggregate_root : Specification
{
    AggregateRootEventHandlersFactory factory;
    IAggregateRootEventHandlers result;
    IEventTypes event_types;

    void Establish()
    {
        event_types = new EventTypesForSpecifications();
        factory = new(event_types);
    }

    void Because() => result = factory.CreateFor(new StatelessAggregateRoot());

    [Fact] void should_the_default_implementation() => result.ShouldBeOfExactType<AggregateRootEventHandlers>();
}
