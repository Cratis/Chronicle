// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventLog;

public class when_getting_tail_sequence_number_for_a_reactor_with_a_claimed_return_type : for_EventSequence.given.all_dependencies
{
    EventLog _eventLog;
    EventSequenceNumber _result;
    Contracts.Sequences.TailSequenceNumberRequest _request;

    void Establish()
    {
        _eventTypes.AllClrTypes.Returns([typeof(MyEvent)]);
        _eventTypes.GetEventTypeFor(typeof(MyEvent)).Returns(typeof(MyEvent).GetEventType());
        var handlers = Substitute.For<IReactorSideEffectHandlers>();
        handlers.CanHandleReturnType(typeof(ClaimedSideEffect)).Returns(true);
        _sequences.TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), CallContext.Default)
            .Returns(call =>
            {
                _request = call.Arg<Contracts.Sequences.TailSequenceNumberRequest>();
                return QueryResult<Contracts.Sequences.EventSequenceTailResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 42 });
            });
        _eventLog = new EventLog(
            "store",
            "default",
            _connection,
            _eventTypes,
            _constraints,
            _eventSerializer,
            _correlationIdAccessor,
            _concurrencyScopeStrategies,
            _causationManager,
            _unitOfWorkManager,
            _identityProvider,
            JsonSerializerOptions.Default,
            handlers);
    }

    async Task Because() => _result = await _eventLog.GetTailSequenceNumberForObserver(typeof(ClaimedReactor));

    [Fact] void should_return_the_tail_number() => _result.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_filter_by_the_reactors_event_type() => _request.EventTypeIds.ShouldContain(typeof(MyEvent).GetEventType().Id.Value);

    class ClaimedReactor : IReactor
    {
        public ClaimedSideEffect Handle(MyEvent @event) => new();
    }

    record ClaimedSideEffect;
}
