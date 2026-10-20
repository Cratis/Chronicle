// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_reporting_handled_events_and_the_observer_times_out_once : Specification
{
    IObserver _observer;
    HandledEventsReport _report;
    Exception _exception;
    int _attempts;

    void Establish()
    {
        _report = new(new Key("p", ArrayIndexers.NoIndexers), new Dictionary<EventTypeId, EventCount> { ["a"] = 2UL }, JobStepId.New(), 42UL);
        _observer = Substitute.For<IObserver>();
        _observer
            .ReportHandledEvents(Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>(), Arg.Any<JobStepId>(), Arg.Any<EventSequenceNumber>())
            .Returns(_ => ++_attempts == 1 ? Task.FromException(new TimeoutException("busy")) : Task.CompletedTask);
    }

    async Task Because() => _exception = await Catch.Exception(() => TransientTimeouts.ReportHandledEventsWithoutFailing(
        _observer,
        _report,
        TimeSpan.Zero,
        Substitute.For<ILogger>(),
        CancellationToken.None));

    [Fact] void should_not_fail_the_step() => _exception.ShouldBeNull();
    [Fact] void should_send_the_same_report_again() => _observer.Received(2).ReportHandledEvents(_report.Partition, _report.Counts, _report.ReportedBy, _report.HandledUpTo);
}
