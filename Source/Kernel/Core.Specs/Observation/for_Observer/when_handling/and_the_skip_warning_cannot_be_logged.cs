// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Logging the new warning fails. The observer carries on exactly as it does without the warning, and the measurement is still taken.
/// </summary>
public class and_the_skip_warning_cannot_be_logged : given.an_observer_with_skip_detection
{
    Exception _exception;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        await _observer.PartitionFailed("P", 100UL, ["Failed handler"], string.Empty);
        _subscriber.ClearReceivedCalls();
        _recordedLogs.ThrowWhen = message => message.Contains("may have skipped", StringComparison.Ordinal);
    }

    async Task Because()
    {
        try
        {
            await _observer.FailedPartitionRecovered("P", 103UL);
            await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_still_skip_the_queued_event() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_still_advance_past_the_queued_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)104UL);
    [Fact] void should_still_clear_the_recovered_failure() => _failedPartitionsStorage.State.IsFailed((Key)"P").ShouldBeFalse();
    [Fact] void should_still_measure_the_suspected_skip() => _suspectedSkips.ShouldEqual(1);
    [Fact] void should_not_have_logged_the_warning() => _recordedLogs.SuspectedSkipWarnings.ShouldBeEmpty();
}
