// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_the_partition_becomes_quarantined : given.an_observer
{
    readonly given.RecordingObserverLogger _logger = new();
    int _warningsBefore;

    protected override ILogger<Observer> CreateLogger() => _logger;

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, MaxRetryAttempts = 1 };

    async Task Establish()
    {
        await _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack");
        _observerAlerts.ClearReceivedCalls();
        _warningsBefore = _logger.Warnings.Count();
    }

    async Task Because()
    {
        await _observer.PartitionFailed("partition", 42UL, ["Failed again"], "Stack");
        await ReportAlerts();
    }

    [Fact] void should_log_one_quarantine_warning() => _logger.Warnings.Skip(_warningsBefore).Count().ShouldEqual(1);
    [Fact] void should_identify_the_quarantine_transition() => _logger.Warnings.Skip(_warningsBefore).Single().Contains("being quarantined", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] async Task should_report_the_new_partition_quarantine() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Single().IsQuarantined && snapshot.FailedPartitions.Single().AttemptCount == 2));
}
