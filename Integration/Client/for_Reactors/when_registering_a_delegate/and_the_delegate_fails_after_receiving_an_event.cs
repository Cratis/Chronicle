// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_registering_a_delegate.and_the_delegate_fails_after_receiving_an_event.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_registering_a_delegate;

[Collection(ChronicleCollection.Name)]
public class and_the_delegate_fails_after_receiving_an_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        readonly TaskCompletionSource<ReactorEvent> _delivered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public ReactorEvent Delivered;
        public IEnumerable<FailedPartition> FailedPartitions;
        public ReactorState ReactorState;

        async Task Because()
        {
            var eventType = EventStore.EventTypes.GetEventTypeFor(typeof(SomeEvent));
            var reactor = await EventStore.Reactors.Register(
                "runtime-delegate-specification",
                builder => builder.WithEventType(eventType),
                (@event, _) =>
                {
                    _delivered.TrySetResult(@event);
                    throw new Exception("Delegate delivery failed");
                });
            await reactor.WaitTillSubscribed();
            await EventStore.EventLog.Append($"runtime-{Guid.NewGuid()}", new SomeEvent(42));
            Delivered = await _delivered.Task.WaitAsync(TimeSpanFactory.FromSeconds(30));
            FailedPartitions = await reactor.WaitForThereToBeFailedPartitions();
            ReactorState = await reactor.GetState();
        }
    }

    [Fact] void should_deliver_the_kernel_event_type_generation() => Context.Delivered.Context.EventType.Generation.Value.ShouldEqual(1u);
    [Fact] void should_deliver_json_without_a_clr_event() => Context.Delivered.Content.Single().Value!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_preserve_the_kernel_generation_map() => Context.Delivered.GenerationalContent[1].ShouldContain("42");
    [Fact] void should_fail_the_partition_by_id() => Context.FailedPartitions.Single().ObserverId.Value.ShouldEqual("runtime-delegate-specification");
    [Fact] void should_be_available_through_the_reactor_state_api() => Context.ReactorState.Id.Value.ShouldEqual("runtime-delegate-specification");
}
