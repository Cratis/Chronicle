// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRootStateFactory;

public class when_getting_stateful_aggregate_root_with_event_handler_methods : given.an_aggregate_root_factory
{
    StatefulAggregateRoot result;
    EventSourceId event_source_id;
    StateForAggregateRoot state;

    void Establish()
    {
        service_provider.Setup(_ => _.GetService(typeof(StatefulAggregateRoot))).Returns(new StatefulAggregateRoot());
        event_source_id = EventSourceId.New();
        event_handlers.Setup(_ => _.HasHandleMethods).Returns(true);

        state_providers.Setup(_ => _.CreateFor(IsAny<AggregateRoot>())).ReturnsAsync(state_provider.Object);

        state = new StateForAggregateRoot(Guid.NewGuid().ToString());
        state_provider.Setup(_ => _.Provide()).ReturnsAsync(state);
    }

    async Task Because() => result = await factory.Get<StatefulAggregateRoot>(event_source_id);

    [Fact] void should_return_instance() => result.ShouldNotBeNull();
    [Fact] void should_set_event_sequence() => result.EventSequence.ShouldEqual(event_sequence.Object);
    [Fact] void should_set_causation_manager() => result.CausationManager.ShouldEqual(causation_manager.Object);
    [Fact] void should_set_event_handlers() => result.EventHandlers.ShouldEqual(event_handlers.Object);
    [Fact] void should_set_event_source_id() => result._eventSourceId.ShouldEqual(event_source_id);
    [Fact] void should_not_get_events() => event_sequence.Verify(_ => _.GetForEventSourceIdAndEventTypes(event_source_id, event_types), Never);
    [Fact] void should_not_handle_events() => event_handlers.Verify(_ => _.Handle(result, IsAny<IEnumerable<EventAndContext>>()), Never);
    [Fact] void should_set_state() => result.GetState().ShouldEqual(state);
    [Fact] void should_call_on_activate() => result.OnActivateCount.ShouldEqual(1);
}
