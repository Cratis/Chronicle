// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Event 42 is being delivered live when a catch-up job reports back. The report interleaves with the delivery and starts the
/// next catch-up in the same turn. That catch-up must not start at or below 42, or event 42 reaches the subscriber twice: once
/// live and once from the catch-up.
/// </summary>
public class and_catch_up_concludes_while_the_subscriber_is_handling : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly JobId _finishingJob = JobId.New();
    readonly TaskCompletionSource<ObserverSubscriberResult> _subscriberResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _delivering = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _catchUpRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly SignallingLogger _logs = new();
    readonly List<EventSequenceNumber> _catchUpsFrom = [];

    void Establish()
    {
        var finishing = new JobState
        {
            Id = _finishingJob,
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
        };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult<IImmutableList<JobState>>([finishing]));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(call =>
            {
                _catchUpsFrom.Add(call.Arg<CatchUpObserverRequest>().FromEventSequenceNumber);
                _catchUpRequested.TrySetResult();
                return Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New()));
            });
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)42UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(call => call.Arg<EventSequenceNumber>() <= 42UL ? (EventSequenceNumber)42UL : EventSequenceNumber.Unavailable);
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_ =>
            {
                _delivering.TrySetResult();
                return _subscriberResult.Task;
            });
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 42UL, LastHandledEventSequenceNumber = 41UL };
    }

    async Task Because()
    {
        var handling = _observer.Handle("ordinary-0", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);
        await _delivering.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var caughtUp = _observer.CaughtUp(_finishingJob, 41UL, 41UL);

        // Either the catch-up is requested while the delivery is still in flight, or it waits for the delivery to settle.
        await Task.WhenAny(_catchUpRequested.Task, _logs.WaitedForLiveDeliveries.Task).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _subscriberResult.SetResult(ObserverSubscriberResult.Ok(42UL));
        await handling;
        await caughtUp;
    }

    [Fact] void should_deliver_the_event_live_once() => _subscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_start_a_catch_up_at_or_below_the_live_delivered_event() => _catchUpsFrom.ShouldEachConformTo(_ => _ > 42UL);
    [Fact] void should_move_past_the_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);

    protected override ILogger<Observer> CreateLogger() => _logs;

    sealed class SignallingLogger : ILogger<Observer>
    {
        public TaskCompletionSource WaitedForLiveDeliveries { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter(state, exception).StartsWith("Waiting for", StringComparison.Ordinal))
            {
                WaitedForLiveDeliveries.TrySetResult();
            }
        }
    }
}
