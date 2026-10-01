// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Configuration;

using AlertsOptions = Cratis.Chronicle.Configuration.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertConditions.when_getting_conditions;

/// <summary>
/// Disabling alerts as a whole disables every condition, even one that is enabled in its own entry.
/// </summary>
public class and_alerts_are_disabled : given.alert_conditions
{
    AlertCondition _failing;
    AlertCondition _quarantined;

    void Establish() => Configure(new AlertsOptions
    {
        Enabled = false,
        Conditions = new Dictionary<string, AlertConditionOptions> { ["partition-failing"] = new() { Enabled = true } }
    });

    void Because()
    {
        _failing = _conditions.For(AlertConditionKind.PartitionFailing);
        _quarantined = _conditions.For(AlertConditionKind.ObserverQuarantined);
    }

    [Fact] void should_disable_a_condition_with_an_entry() => _failing.Enabled.ShouldBeFalse();
    [Fact] void should_disable_a_condition_without_an_entry() => _quarantined.Enabled.ShouldBeFalse();
}
