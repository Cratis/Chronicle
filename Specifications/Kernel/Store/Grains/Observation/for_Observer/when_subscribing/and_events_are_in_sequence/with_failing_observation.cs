// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_subscribing.and_events_are_in_sequence;

public class with_failing_observation : given.a_connected_observer_and_two_event_types_and_one_event_in_sequence
{
    void Establish()
    {
        partitioned_observer.Setup(_ => _.OnNext(appended_event)).Callback((AppendedEvent _) => throw new NotImplementedException());
    }

    async Task Because() => await observer.Subscribe(event_types);

    [Fact] void should_not_modify_offset() => state.Offset.Value.ShouldEqual(0u);
    [Fact] void should_not_modify_last_handled() => state.Offset.Value.ShouldEqual(0u);
    [Fact] void should_fail_the_partition() => state.IsPartitionFailed(event_source_id).ShouldBeTrue();
    [Fact] void should_register_reminder() => reminder_registry.Verify(_ => _.RegisterOrUpdateReminder(Observer.RecoverReminder, IsAny<TimeSpan>(), IsAny<TimeSpan>()), Once());
}
