// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_registering_a_delegate.and_subscribing_to_two_generations.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_registering_a_delegate;

[Collection(ChronicleCollection.Name)]
public class and_subscribing_to_two_generations(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        readonly TaskCompletionSource<ReactorEvent> _first = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<ReactorEvent> _second = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override IEnumerable<Type> EventTypes => [typeof(RuntimeGenerationEventV1), typeof(RuntimeGenerationEvent)];
        public ReactorEvent First;
        public ReactorEvent Second;

        async Task Because()
        {
            var eventType = EventStore.EventTypes.GetEventTypeFor(typeof(RuntimeGenerationEvent));
            var reactor = await EventStore.Reactors.Register(
                "runtime-two-generations",
                builder => builder.WithEventType(eventType with { Generation = 1 }).WithEventType(eventType),
                (@event, _) =>
                {
                    if (@event.Context.EventType.Generation.Value == 1)
                    {
                        _first.TrySetResult(@event);
                    }
                    else
                    {
                        _second.TrySetResult(@event);
                    }
                    return Task.CompletedTask;
                });
            await reactor.WaitTillSubscribed();
            await EventStore.EventLog.AppendMany($"runtime-{Guid.NewGuid()}", [new RuntimeGenerationEventV1(1), new RuntimeGenerationEvent(2)]);
            First = await _first.Task.WaitAsync(TimeSpanFactory.FromSeconds(30));
            Second = await _second.Task.WaitAsync(TimeSpanFactory.FromSeconds(30));
        }
    }

    [Fact] void should_receive_generation_one() => Context.First.Content.Single().Value!.GetValue<int>().ShouldEqual(1);
    [Fact] void should_receive_generation_two() => Context.Second.Content.Single().Value!.GetValue<int>().ShouldEqual(2);
}
