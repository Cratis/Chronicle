// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

using ContractAppendedEvent = Cratis.Chronicle.Contracts.Events.AppendedEvent;
using ContractReactors = Cratis.Chronicle.Contracts.Observation.Reactors.IReactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_to_a_delegate;

public class and_two_events_share_a_batch : given.all_dependencies
{
    readonly TaskCompletionSource<ReactorResult> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<IImmutableList<Causation>> _chains = [];
    Subject<EventsToObserve> _observed;

    protected override ICausationManager CreateCausationManager() => new CausationManager();

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
                    if (message.Content.Value is ReactorResult result)
                    {
                        _result.TrySetResult(result);
                    }
                });
                return _observed;
            });
    }

    async Task Because()
    {
        await _reactors.Register("bridge", builder => builder.WithEventType(new EventType("orders", 1)), (_, _) =>
        {
            _chains.Add(_causationManager.GetCurrentChain());
            return Task.CompletedTask;
        });
        _observed.OnNext(new EventsToObserve
        {
            Partition = "order-42",
            Events = new[] { 12UL, 13UL }.Select(sequenceNumber => new ContractAppendedEvent
            {
                Context = (EventContext.Empty with { EventType = new EventType("orders", 1), SequenceNumber = sequenceNumber }).ToContract(),
                Content = "{\"order\":42}"
            }).ToArray()
        });
        await _result.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_record_only_the_second_events_causation() => _chains[1].Where(_ => _.Type == ReactorHandler.CausationType)
        .Select(_ => _.Properties[ReactorHandler.CausationEventSequenceNumberProperty]).ShouldContainOnly(["13"]);
}
