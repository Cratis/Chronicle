// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;
using CatchResult = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_method_returning_reactor_side_effect_to_specific_event_sequence : Specification
{
    CatchResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventSequence _customSequence;
    EventSequenceId _customSequenceId;
    EventContext _eventContext;
    MyOutboundEvent _outboundEvent;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _outboundEvent = new MyOutboundEvent();
        _customSequenceId = EventSequenceId.Log;

        _customSequence = Substitute.For<IEventSequence>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.GetEventSequence(_customSequenceId).Returns(_customSequence);

        var reactor = new ReactorWithCustomSequenceSideEffect(_outboundEvent, _customSequenceId);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithCustomSequenceSideEffect),
            new ActivatedArtifact(reactor, typeof(ReactorWithCustomSequenceSideEffect), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            _eventStore);

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_append_to_the_custom_sequence() =>
        _customSequence.Received(1).Append(_eventContext.EventSourceId, _outboundEvent, default, default, default, default, default, default, default, default);

    class ReactorWithCustomSequenceSideEffect(MyOutboundEvent outbound, EventSequenceId sequenceId) : IReactor
    {
        public Task<ReactorSideEffect> Handle(MyEvent @event) =>
            Task.FromResult(new ReactorSideEffect { Event = outbound, EventSequenceId = sequenceId });
    }
}
