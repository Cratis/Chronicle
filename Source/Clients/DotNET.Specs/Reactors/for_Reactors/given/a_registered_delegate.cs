// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

using ContractAppendedEvent = Cratis.Chronicle.Contracts.Events.AppendedEvent;
using ContractReactors = Cratis.Chronicle.Contracts.Observation.Reactors.IReactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.given;

public class a_registered_delegate : all_dependencies
{
    protected readonly TaskCompletionSource<ReactorEvent> _received = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource<ReactorResult> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected Subject<EventsToObserve> _observed;
    protected ReactorDefinition _definition;
    protected readonly List<ReactorDefinition> _definitions = [];
    protected IReactorHandler _handler;

    void Establish()
    {
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
                            _definition = registered.Reactor;
                            _definitions.Add(registered.Reactor);
                            break;
                        case ReactorResult result:
                            _result.TrySetResult(result);
                            break;
                    }
                });
                return _observed;
            });
        _handler = _reactors.Register(
            "bridge",
            builder => builder.WithEventType(new EventType("orders", 1)).WithEventType(new EventType("orders", 2)).NotReplayable(),
            async (@event, _) =>
            {
                _received.TrySetResult(@event);
                await _release.Task;
            }).GetAwaiter().GetResult();
    }

    protected void Deliver() => _observed.OnNext(new EventsToObserve
    {
        Partition = "order-42",
        Events = [new ContractAppendedEvent
        {
            Context = (EventContext.Empty with { EventType = new EventType("orders", 1), SequenceNumber = 12 }).ToContract(),
            Content = "{\"order\":42}",
            GenerationalContent = new Dictionary<int, string>
            {
                [1] = "{\"order\":42}",
                [2] = "{\"orderId\":42}"
            }
        }]
    });
}
