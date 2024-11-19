// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootEventHandlersFactory;

public class when_getting_for_an_aggregate_root : Specification
{
    AggregateRootEventHandlersFactory _factory;
    IAggregateRootEventHandlers _result;
    IEventTypes _eventTypes;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _factory = new(_eventTypes);
    }

    void Because() => _result = _factory.GetFor(new StatefulAggregateRoot());

    [Fact] void should_the_default_implementation() => _result.ShouldBeOfExactType<AggregateRootEventHandlers>();
}
