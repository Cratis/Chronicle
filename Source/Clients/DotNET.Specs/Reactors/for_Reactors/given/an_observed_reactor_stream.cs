// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

using ContractAppendedEvent = Cratis.Chronicle.Contracts.Events.AppendedEvent;
using ContractReactors = Cratis.Chronicle.Contracts.Observation.Reactors.IReactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.given;

/// <summary>
/// A reactors instance whose observation stream the spec feeds, knowing one event type in two generations.
/// </summary>
public class an_observed_reactor_stream : all_dependencies
{
    protected static readonly EventType _orderPlacedV1 = new(nameof(OrderPlaced), 1);
    protected static readonly EventType _orderPlaced = new(nameof(OrderPlaced), 2);
    protected readonly TaskCompletionSource<ReactorResult> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly List<ReactorDefinition> _definitions = [];
    protected readonly List<JsonObject> _deserialized = [];
    protected readonly OrderPlaced _event = new("42");
    protected Subject<EventsToObserve> _observed;

    void Establish()
    {
        _eventTypes.GetEventTypeFor(typeof(OrderPlaced)).Returns(_orderPlaced);
        _eventTypes.GetClrTypeFor(_orderPlaced.Id, Arg.Any<EventTypeGeneration>()).Returns(typeof(OrderPlaced));
        _eventSerializer.Deserialize(typeof(OrderPlaced), Arg.Any<JsonObject>()).Returns(call =>
        {
            _deserialized.Add(call.Arg<JsonObject>());
            return _event;
        });

        _observed = new Subject<EventsToObserve>();
        var reactors = Substitute.For<ContractReactors>();
        _services.Reactors.Returns(reactors);
        reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>())
            .Returns(call =>
            {
                call.Arg<IObservable<ReactorMessage>>().Subscribe(message =>
                {
                    switch (message.Content.Value)
                    {
                        case RegisterReactor registered:
                            _definitions.Add(registered.Reactor);
                            break;
                        case ReactorResult result:
                            _result.TrySetResult(result);
                            break;
                    }
                });
                return _observed;
            });
    }

    protected void Deliver(EventType appendedAs, string content, Dictionary<int, string>? generationalContent = null) => _observed.OnNext(new EventsToObserve
    {
        Partition = "order-42",
        Events = [new ContractAppendedEvent
        {
            Context = (EventContext.Empty with { EventType = appendedAs, SequenceNumber = 12 }).ToContract(),
            Content = content,
            GenerationalContent = generationalContent ?? []
        }]
    });

    [EventType(generation: 2)]
    public record OrderPlaced(string OrderNumber);
}
