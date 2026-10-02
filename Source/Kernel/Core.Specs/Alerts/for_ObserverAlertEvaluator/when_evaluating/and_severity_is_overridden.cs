// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

using AlertsOptions = Cratis.Chronicle.Configuration.Alerts;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.when_evaluating;

/// <summary>
/// The severity a condition is raised with comes from the configuration. Here a failing partition is raised as
/// critical and a quarantined observer as a warning.
/// </summary>
public class and_severity_is_overridden : given.an_evaluator
{
    ObserverAlertEvaluation _result;

    void Establish() => Configure(new AlertsOptions
    {
        Conditions = new Dictionary<string, AlertConditionOptions>
        {
            ["partition-failing"] = new() { Severity = AlertSeverity.Critical },
            ["observer-quarantined"] = new() { Severity = AlertSeverity.Warning }
        }
    });

    void Because() => _result = Evaluate(SnapshotOf(FailedPartition(FailedPartitionId.New(), TimeSpan.FromMinutes(10))) with { IsQuarantined = true });

    [Fact] void should_raise_the_failing_partition_with_the_configured_severity() => _result.Transitions.OfType<AlertRaised>().Single(_ => _.Condition == AlertConditionKind.PartitionFailing).Severity.ShouldEqual(AlertSeverity.Critical);
    [Fact] void should_raise_the_quarantined_observer_with_the_configured_severity() => _result.Transitions.OfType<AlertRaised>().Single(_ => _.Condition == AlertConditionKind.ObserverQuarantined).Severity.ShouldEqual(AlertSeverity.Warning);
}
