// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsWithConcurrencyScopesResultHandler.when_handling;

public class with_two_guards_for_one_source : Specification
{
    IEventStore _store;
    KeyValuePair<EventSourceId, ConcurrencyScope>[] _sent;
    Result<ReactorSideEffectFailure> _result;

    void Establish()
    {
        _store = Substitute.For<IEventStore>();
        var log = Substitute.For<IEventLog>();
        _store.EventLog.Returns(log);
        log.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>>()).Returns(call =>
            {
                _sent = call.Arg<IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>>().ToArray();
                return AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]);
            });
    }

    async Task Because() => _result = await new EventsWithConcurrencyScopesResultHandler().Handle(
        new(EventContext.Empty, new object(), ReactorContextValues.Empty),
        _store,
        new EventsWithConcurrencyScopes(
            [new("report", new object())],
            [new("report", new ConcurrencyScope(7, "report", "first")), new("report", new ConcurrencyScope(9, "report", "second"))]));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_send_both_guards() => _sent.Length.ShouldEqual(2);
    [Fact] void should_preserve_the_expectations() => _sent.Select(_ => _.Value.SequenceNumber).ShouldContainOnly((EventSequenceNumber)7UL, (EventSequenceNumber)9UL);
}
