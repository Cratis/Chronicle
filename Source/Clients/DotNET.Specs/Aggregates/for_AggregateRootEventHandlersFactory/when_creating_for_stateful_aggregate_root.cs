// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootEventHandlersFactory;

public class when_creating_for_stateful_aggregate_root : Specification
{
    AggregateRootEventHandlersFactory factory;
    IAggregateRootEventHandlers result;
    Mock<IEventTypes> event_types;

    void Establish()
    {
        event_types = new();
        factory = new(event_types.Object);
    }

    void Because() => result = factory.CreateFor(new StatefulAggregateRoot());

    [Fact] void should_return_a_null_implementation() => result.ShouldBeOfExactType<NullAggregateRootEventHandlers>();
}
