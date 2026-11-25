// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Registrations.for_RegistrationBacklog;

public class when_registrations_wait : Specification
{
    ManualTimeProvider _clock;
    RegistrationBacklog _backlog;
    Measurement<int>[] _pending;
    Measurement<double>[] _ageWhileWaiting;
    Measurement<double>[] _ageOfADrainedStore;
    Measurement<int>[] _pendingAfterForgetting;

    void Establish()
    {
        _clock = new();
        _backlog = new((Meter?)null, _clock);
    }

    void Because()
    {
        var acceptedAt = _clock.GetUtcNow();
        _clock.Advance(TimeSpan.FromSeconds(90));
        _backlog.Report("busy-store", 3, acceptedAt);
        _backlog.Report("drained-store", 0, acceptedAt);
        _pending = [.. _backlog.ObservePending()];
        _ageWhileWaiting = [.. _backlog.ObserveOldestPendingAge()];
        _ageOfADrainedStore = _ageWhileWaiting;
        _backlog.Forget("busy-store");
        _pendingAfterForgetting = [.. _backlog.ObservePending()];
    }

    [Fact] void should_report_how_many_wait_per_event_store() => _pending.Single(_ => StoreOf(_.Tags) == "busy-store").Value.ShouldEqual(3);
    [Fact] void should_report_how_long_the_oldest_has_waited() => _ageWhileWaiting.Single(_ => StoreOf(_.Tags) == "busy-store").Value.ShouldEqual(90d);
    [Fact] void should_report_zero_age_for_a_store_that_has_drained() => _ageOfADrainedStore.Single(_ => StoreOf(_.Tags) == "drained-store").Value.ShouldEqual(0d);
    [Fact] void should_keep_reporting_a_drained_store_as_zero() => _pending.Single(_ => StoreOf(_.Tags) == "drained-store").Value.ShouldEqual(0);
    [Fact] void should_stop_reporting_a_forgotten_store() => _pendingAfterForgetting.Select(_ => StoreOf(_.Tags)).ShouldContainOnly("drained-store");

    static string StoreOf(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == WellKnownTelemetryAttributes.EventStoreName)
            {
                return (string)tag.Value!;
            }
        }

        return string.Empty;
    }
}
