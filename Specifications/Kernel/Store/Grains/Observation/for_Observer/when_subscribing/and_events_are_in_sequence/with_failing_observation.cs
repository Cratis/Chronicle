// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_subscribing.and_events_are_in_sequence;

public class with_failing_observation : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    void Establish()
    {
        observer_stream.Setup(_ => _.OnNextAsync(appended_event, IsAny<StreamSequenceToken>())).Callback((AppendedEvent _, StreamSequenceToken __) => throw new NotImplementedException());
    }

    async Task Because()
    {
        await observer.Subscribe(event_types, observer_namespace);
        await observers[0].OnNextAsync(appended_event);
    }

    [Fact] void should_set_current_namespace_in_state() => state.CurrentNamespace.ShouldEqual(observer_namespace);
    [Fact] void should_not_modify_offset() => state.Offset.Value.ShouldEqual(0u);
    [Fact] void should_not_modify_last_handled() => state.Offset.Value.ShouldEqual(0u);
    [Fact] void should_fail_the_partition() => state.IsPartitionFailed(event_source_id).ShouldBeTrue();
    [Fact] void should_register_reminder() => reminder_registry.Verify(_ => _.RegisterOrUpdateReminder(Observer.RecoverReminder, IsAny<TimeSpan>(), IsAny<TimeSpan>()), Once());
}
