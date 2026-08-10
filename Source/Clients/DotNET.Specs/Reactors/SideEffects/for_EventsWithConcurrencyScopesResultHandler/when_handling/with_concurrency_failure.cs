// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsWithConcurrencyScopesResultHandler.when_handling;

public class with_concurrency_failure : Specification
{
    Result<ReactorSideEffectFailure> _result;
    IEventLog _eventLog;
    IEventStore _eventStore;
    EventSourceId _firstTarget;
    EventSourceId _secondTarget;
    ReactorSideEffectFailure _failure;

    void Establish()
    {
        _firstTarget = EventSourceId.New();
        _secondTarget = EventSourceId.New();
        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendMany(
                Arg.Any<IEnumerable<EventForEventSourceId>>(),
                concurrencyScopes: Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .ReturnsForAnyArgs(AppendManyResult.Failed(
                CorrelationId.New(),
                [new ConcurrencyViolation(_firstTarget, new(41), new(42))]));
    }

    async Task Because()
    {
        _result = await new EventsWithConcurrencyScopesResultHandler().Handle(
            new(Events.EventContext.Empty, new object(), ReactorContextValues.Empty),
            _eventStore,
            new EventsWithConcurrencyScopes(
                [new(_firstTarget, new object()), new(_secondTarget, new object())],
                [new(_firstTarget, new ConcurrencyScope(new(41)))]));
        _result.TryGetError(out _failure);
    }

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_concurrency_failure() => _failure.AppendFailures.Single().HasConcurrencyViolation.ShouldBeTrue();
    [Fact] void should_capture_the_first_target() => _failure.GetTargetEventSourceIds().ShouldContain(_firstTarget);
    [Fact] void should_capture_the_second_target() => _failure.GetTargetEventSourceIds().ShouldContain(_secondTarget);
    [Fact] void should_submit_only_one_append() => _eventLog.Received(1).AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>());
    [Fact] void should_not_append_events_individually() =>
        _eventLog.DidNotReceiveWithAnyArgs().Append(default!, default!, default, default, default, default, default, default, default, default);
}
