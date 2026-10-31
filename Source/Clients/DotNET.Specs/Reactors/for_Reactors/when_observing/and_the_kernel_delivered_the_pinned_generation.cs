// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_observing;

public class and_the_kernel_delivered_the_pinned_generation : given.all_dependencies
{
    Subject<EventsToObserve> _observations;
    readonly TaskCompletionSource<ReactorEvent> _received = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ReactorResult> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ReactorEvent _event;
    protected override Observation.EventGenerationDelivery GenerationDelivery => Observation.EventGenerationDelivery.Pinned;

    async Task Establish()
    {
        _observations = new();
        _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReactorMessage>>().Subscribe(message =>
            {
                if (message.Content.Value is ReactorResult result)
                {
                    _result.TrySetResult(result);
                }
            });
            return _observations;
        });
        await _reactors.Register("pinned", builder => builder.WithEventType(new EventType("person-registered", 2)), (@event, _) =>
        {
            _received.TrySetResult(@event);
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        _observations.OnNext(new EventsToObserve
        {
            Partition = "person",
            DeliveryId = 42,
            Events = [new Contracts.Events.AppendedEvent
            {
                Context = (EventContext.Empty with { EventType = new("person-registered", 2), AppendedGeneration = 1 }).ToContract(),
                Content = "{\"fullName\":\"Ada Lovelace\"}",
                GenerationalContent = new Dictionary<int, string> { [2] = "{\"fullName\":\"ciphertext\"}" }
            }]
        });
        _event = await _received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _result.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_use_the_released_content_not_the_protected_generation_map() => _event.Content["fullName"]!.GetValue<string>().ShouldEqual("Ada Lovelace");
    [Fact] void should_keep_the_appended_generation() => _event.Context.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] async Task should_echo_the_delivery_id() => (await _result.Task).DeliveryId.ShouldEqual(42UL);
}
