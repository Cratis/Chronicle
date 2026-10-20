// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class and_the_handler_returns_an_event : Specification
{
    readonly List<IdempotencyKey> _keysAtEpochThree = [];
    readonly List<IdempotencyKey> _keysAtEpochZero = [];
    IEventTypes _eventTypes;
    IEventStore _eventStore;
    IEventLog _eventLog;
    List<IdempotencyKey> _current;
    EventContext _eventContext;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventTypes.Returns(_eventTypes);
        _eventLog = Substitute.For<IEventLog>();
        _eventLog.SupportsIdempotentAppends.Returns(true);
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendOnce(default!, default!, default!, default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(callInfo =>
            {
                _current.Add(callInfo.ArgAt<IdempotencyKey>(2));
                return AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First);
            });
        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because()
    {
        _current = _keysAtEpochThree;
        var epochThree = CreateInvoker(new EpochThreeReactor());
        await epochThree.Invoke(new MyEvent(), _eventContext);
        await epochThree.Invoke(new MyEvent(), _eventContext);

        _current = _keysAtEpochZero;
        await CreateInvoker(new EpochZeroReactor()).Invoke(new MyEvent(), _eventContext);
    }

    [Fact] void should_append_once_per_delivery() => _keysAtEpochThree.Count.ShouldEqual(2);
    [Fact] void should_derive_the_same_key_when_the_delivery_repeats() => _keysAtEpochThree[0].ShouldEqual(_keysAtEpochThree[1]);
    [Fact] void should_derive_another_key_in_another_epoch() => _keysAtEpochZero.Single().ShouldNotEqual(_keysAtEpochThree[0]);

    ReactorInvoker CreateInvoker(IReactor reactor) =>
        new(
            _eventTypes,
            Substitute.For<IReactorMiddlewares>(),
            reactor.GetType(),
            new ActivatedArtifact(reactor, reactor.GetType(), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventResultHandler()])),
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications())
        {
            RegisteredId = "invoker-epoch-reactor"
        };

    [Reactor("invoker-epoch-reactor", effectEpoch: 3)]
    class EpochThreeReactor : IReactor
    {
        public MyOutboundEvent Handle(MyEvent @event) => new();
    }

    [Reactor("invoker-epoch-reactor")]
    class EpochZeroReactor : IReactor
    {
        public MyOutboundEvent Handle(MyEvent @event) => new();
    }
}
