// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_bulk_failures;

public class and_a_later_partition_cannot_be_recorded : given.an_observer
{
    Exception _error;

    void Establish()
    {
        var calls = 0;
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(_ => ++calls == 2 ? throw new InvalidOperationException("Configuration unavailable") : _observersConfig);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _observer.PartitionsFailed([new("first", 42UL), new("second", 43UL)]));
        await ReportAlerts();
    }

    [Fact] void should_surface_partial_failure() => _error.ShouldNotBeNull();
    [Fact] void should_have_persisted_the_first_partition_independently() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] async Task should_leave_the_committed_partial_level_reportable() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 1 && _.FailedPartitions.Single().Partition == "first"));
}
