// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_reporting_a_batch_and_the_observer_times_out_once : Specification
{
    IObserver _observer;
    HandledEventsReport[] _reports;
    int _attempts;

    void Establish()
    {
        var step = JobStepId.New();
        _reports = [new("partition", new Dictionary<EventTypeId, EventCount> { ["event"] = 2UL }, step, 42UL)];
        _observer = Substitute.For<IObserver>();
        _observer.ReportHandledEventsBatch(step, _reports).Returns(_ => ++_attempts == 1 ? Task.FromException(new TimeoutException("busy")) : Task.CompletedTask);
    }

    async Task Because() => await TransientTimeouts.ReportHandledEventsBatchWithoutFailing(_observer, _reports[0].ReportedBy, _reports, TimeSpan.Zero, Substitute.For<ILogger>(), CancellationToken.None);

    [Fact] void should_retry_the_identical_batch() => _observer.Received(2).ReportHandledEventsBatch(_reports[0].ReportedBy, _reports);
}
