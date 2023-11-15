// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRoot;

public class when_committing : given.a_stateless_aggregate_root
{
    FirstEventType first_event;
    SecondEventType second_event;

    EventSourceId event_source_id_to_commit;
    IEnumerable<object> events_to_commit;
    AggregateRootCommitResult result;

    void Establish()
    {
        first_event = new("First");
        second_event = new("Second");

        event_sequence.Setup(_ => _.AppendMany(IsAny<EventSourceId>(), IsAny<IEnumerable<object>>())).Returns((EventSourceId eventSourceId, IEnumerable<object> events) =>
        {
            event_source_id_to_commit = eventSourceId;
            events_to_commit = events;
            return Task.CompletedTask;
        });

        aggregate_root.Apply(first_event);
        aggregate_root.Apply(second_event);
    }

    async Task Because() => result = await aggregate_root.Commit();

    [Fact] void should_append_for_correct_event_source_id() => event_source_id_to_commit.ShouldEqual(aggregate_root._eventSourceId);
    [Fact] void should_append_events_to_event_sequence() => events_to_commit.ShouldContainOnly(first_event, second_event);
    [Fact] void should_return_successful_result() => result.Success.ShouldBeTrue();
    [Fact] void should_contain_events_in_result() => result.Events.ShouldContainOnly(first_event, second_event);
    [Fact] void should_add_causation() => causation_manager.Verify(_ => _.Add(AggregateRoot.CausationType, IsAny<IDictionary<string, string>>()));
    [Fact] void should_dehydrate_state() => state_provider.Verify(_ => _.Dehydrate(), Once);
}
