// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Configuration;

using AlertsOptions = Cratis.Chronicle.Configuration.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertConditions.when_getting_conditions;

/// <summary>
/// Configuration is merged over the defaults per condition and per setting: what an entry sets wins, what it leaves
/// out keeps the default, and conditions without an entry are untouched. The keys are not case sensitive.
/// </summary>
public class and_a_condition_is_configured : given.alert_conditions
{
    AlertCondition _failing;
    AlertCondition _exhausted;
    AlertCondition _quarantined;

    void Establish() => Configure(new AlertsOptions
    {
        Conditions = new Dictionary<string, AlertConditionOptions>
        {
            ["Partition-Failing"] = new() { Severity = AlertSeverity.Critical, RaiseAfter = TimeSpan.FromMinutes(1) },
            ["observer-quarantined"] = new() { Enabled = false }
        }
    });

    void Because()
    {
        _failing = _conditions.For(AlertConditionKind.PartitionFailing);
        _exhausted = _conditions.For(AlertConditionKind.PartitionRetriesExhausted);
        _quarantined = _conditions.For(AlertConditionKind.ObserverQuarantined);
    }

    [Fact] void should_use_the_configured_severity() => _failing.Severity.ShouldEqual(AlertSeverity.Critical);
    [Fact] void should_use_the_configured_raise_after() => _failing.RaiseAfter.ShouldEqual(TimeSpan.FromMinutes(1));
    [Fact] void should_keep_the_condition_enabled_when_the_entry_does_not_say_otherwise() => _failing.Enabled.ShouldBeTrue();
    [Fact] void should_disable_the_configured_condition() => _quarantined.Enabled.ShouldBeFalse();
    [Fact] void should_keep_the_default_severity_when_the_entry_does_not_set_one() => _quarantined.Severity.ShouldEqual(AlertSeverity.Critical);
    [Fact] void should_leave_a_condition_without_an_entry_at_its_defaults() => _exhausted.ShouldEqual(new AlertCondition(AlertConditionKind.PartitionRetriesExhausted, true, AlertSeverity.Critical, TimeSpan.FromMinutes(5)));
}
