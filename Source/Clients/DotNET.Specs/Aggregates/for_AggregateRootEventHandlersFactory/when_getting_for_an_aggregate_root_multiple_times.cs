// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootEventHandlersFactory;

public class when_getting_for_an_aggregate_root_multiple_times : Specification
{
    AggregateRootEventHandlersFactory _factory;
    IAggregateRootEventHandlers _firstResult;
    IAggregateRootEventHandlers _secondResult;
    IEventTypes _eventTypes;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _factory = new(_eventTypes);
    }

    void Because()
    {
        _firstResult = _factory.GetFor(new StatefulAggregateRoot());
        _secondResult = _factory.GetFor(new StatefulAggregateRoot());
    }

    [Fact] void should_return_same_object() => _firstResult.ShouldBeSame(_secondResult);
}
