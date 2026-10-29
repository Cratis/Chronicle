// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsWithConcurrencyScopesResultHandler.when_handling;

public class with_no_events : Specification
{
    Result<ReactorSideEffectFailure> _result;
    IEventLog _eventLog;
    IEventStore _eventStore;

    void Establish()
    {
        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
    }

    async Task Because() => _result = await new EventsWithConcurrencyScopesResultHandler().Handle(
        new(EventContext.Empty, new object(), ReactorContextValues.Empty),
        _eventStore,
        new EventsWithConcurrencyScopes([], []));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_append_an_empty_batch() => _eventLog.DidNotReceive().AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>());
}
