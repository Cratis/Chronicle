// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertConditions.when_getting_conditions;

/// <summary>
/// With no configuration the built-in defaults apply: a failing partition is a warning after five minutes, and
/// retries exhausted and a quarantined observer are critical. All of them are enabled.
/// </summary>
public class and_nothing_is_configured : given.alert_conditions
{
    AlertCondition _failing;
    AlertCondition _exhausted;
    AlertCondition _quarantined;

    void Because()
    {
        _failing = _conditions.For(AlertConditionKind.PartitionFailing);
        _exhausted = _conditions.For(AlertConditionKind.PartitionRetriesExhausted);
        _quarantined = _conditions.For(AlertConditionKind.ObserverQuarantined);
    }

    [Fact] void should_enable_partition_failing() => _failing.Enabled.ShouldBeTrue();
    [Fact] void should_make_partition_failing_a_warning() => _failing.Severity.ShouldEqual(AlertSeverity.Warning);
    [Fact] void should_raise_partition_failing_after_five_minutes() => _failing.RaiseAfter.ShouldEqual(TimeSpan.FromMinutes(5));
    [Fact] void should_enable_retries_exhausted() => _exhausted.Enabled.ShouldBeTrue();
    [Fact] void should_make_retries_exhausted_critical() => _exhausted.Severity.ShouldEqual(AlertSeverity.Critical);
    [Fact] void should_enable_observer_quarantined() => _quarantined.Enabled.ShouldBeTrue();
    [Fact] void should_make_observer_quarantined_critical() => _quarantined.Severity.ShouldEqual(AlertSeverity.Critical);
}
