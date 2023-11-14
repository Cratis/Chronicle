// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRootStateFactory;

public class when_getting_stateless_aggregate_root_with_event_handler_methods : given.an_aggregate_root_factory
{
    StatelessAggregateRoot result;
    EventSourceId event_source_id;
    IEnumerable<object> events_handled;

    void Establish()
    {
        service_provider.Setup(_ => _.GetService(typeof(StatelessAggregateRoot))).Returns(new StatelessAggregateRoot());
        event_source_id = EventSourceId.New();
        event_handlers.Setup(_ => _.HasHandleMethods).Returns(true);
        event_handlers
            .Setup(_ => _.Handle(IsAny<StatelessAggregateRoot>(), IsAny<IEnumerable<EventAndContext>>()))
            .Returns((IAggregateRoot _, IEnumerable<EventAndContext> events) =>
            {
                events_handled = events.Select(_ => _.Event).ToArray();
                return Task.CompletedTask;
            });
    }

    async Task Because() => result = await factory.Get<StatelessAggregateRoot>(event_source_id);

    [Fact] void should_return_instance() => result.ShouldNotBeNull();
    [Fact] void should_set_event_sequence() => result.EventSequence.ShouldEqual(event_sequence.Object);
    [Fact] void should_set_causation_manager() => result.CausationManager.ShouldEqual(causation_manager.Object);
    [Fact] void should_set_event_handlers() => result.EventHandlers.ShouldEqual(event_handlers.Object);
    [Fact] void should_set_event_source_id() => result._eventSourceId.ShouldEqual(event_source_id);
    [Fact] void should_get_events() => event_sequence.Verify(_ => _.GetForEventSourceIdAndEventTypes(event_source_id, event_types), Once);
    [Fact] void should_handle_events() => event_handlers.Verify(_ => _.Handle(result, IsAny<IEnumerable<EventAndContext>>()), Once);
    [Fact] void should_handle_correct_events() => events_handled.ShouldContainOnly(first_event, second_event);
    [Fact] void should_not_get_state_from_state_provider() => state_provider.Verify(_ => _.Provide(), Never);
    [Fact] void should_call_on_activate() => result.OnActivateCount.ShouldEqual(1);
}
