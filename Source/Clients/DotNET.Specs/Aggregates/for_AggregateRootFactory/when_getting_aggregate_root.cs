// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootFactory;

public class when_getting_aggregate_root : given.an_aggregate_root_factory
{
    StatelessAggregateRoot _result;
    EventSourceId _eventSourceId;

    void Establish() => _eventSourceId = EventSourceId.New();

    async Task Because() => _result = await _factory.Get<StatelessAggregateRoot>(_eventSourceId, true);

    [Fact] void should_return_an_instance() => _result.ShouldNotBeNull();
    [Fact] void should_return_an_instance_of_the_correct_type() => _result.ShouldBeOfExactType<StatelessAggregateRoot>();
    [Fact] void should_set_context_with_event_source_id() => _result._context.EventSourceId.ShouldEqual(_eventSourceId);
    [Fact] void should_set_context_with_event_sequence() => _result._context.EventSequence.ShouldEqual(_eventSequence);
    [Fact] void should_set_context_with_aggregate_root() => _result._context.AggregateRoot.ShouldEqual(_result);
    [Fact] void should_set_context_with_correct_auto_commit() => _result._context.AutoCommit.ShouldBeTrue();
    [Fact] void should_set_mutation() => _result._mutation.ShouldNotBeNull();
    [Fact] void should_rehydrate() => _mutator.Received(1).Rehydrate();
    [Fact] void should_call_on_activate_once() => _result.OnActivateCount.ShouldEqual(1);
}
