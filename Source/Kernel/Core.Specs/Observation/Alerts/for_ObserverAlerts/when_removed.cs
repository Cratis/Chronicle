// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts;

public class when_removed : given.an_alert_tracker
{
    void Establish() => GivenHistory(
        RaisedForSnapshot(),
        new AlertRaised(IncidentId.New(), AlertConditionKind.ObserverQuarantined, AlertSeverity.Critical, AlertTarget.For(_key, AlertPartition.None), RaisedForSnapshot().Evidence));

    async Task Because() => await ReconcileRemoval();

    [Fact] void should_clear_both_partition_and_quarantine_incidents() => _appends.OfType<AlertCleared>().Count().ShouldEqual(2);
    [Fact] void should_clear_only_as_removed() => _appends.OfType<AlertCleared>().All(clear => clear.Reason == AlertClearedReason.Removed).ShouldBeTrue();
    [Fact] void should_not_raise_any_incidents() => _appends.OfType<AlertRaised>().ShouldBeEmpty();
}
