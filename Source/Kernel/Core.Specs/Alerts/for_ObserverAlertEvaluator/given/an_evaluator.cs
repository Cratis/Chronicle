// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using AlertsOptions = Cratis.Chronicle.Configuration.Alerts;

namespace Cratis.Chronicle.Alerts.for_ObserverAlertEvaluator.given;

public class an_evaluator : Specification
{
    protected static readonly DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    protected static readonly TimeSpan _graceTime = TimeSpan.FromMinutes(5);
    protected static readonly Guid _defaultQuarantineEpisodeId = Guid.NewGuid();

    protected ObserverAlertEvaluator _evaluator;
    protected ObserverKey _observer;

    void Establish()
    {
        _observer = new("orders-projection", "store", "namespace", EventSequenceId.Log);
        Configure(new AlertsOptions());
    }

    protected void Configure(AlertsOptions alerts)
    {
        var options = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        options.CurrentValue.Returns(new ChronicleOptions { Alerts = alerts });
        _evaluator = new(new AlertConditions(options, NullLogger<AlertConditions>.Instance));
    }

    protected static AlertsOptions AlertsWith(string condition, AlertConditionOptions options) =>
        new() { Conditions = new Dictionary<string, AlertConditionOptions>(StringComparer.OrdinalIgnoreCase) { [condition] = options } };

    protected static FailedPartitionSnapshot FailedPartition(
        FailedPartitionId id,
        TimeSpan failingFor,
        bool isQuarantined = false,
        int attemptCount = 3,
        int? attemptsInCurrentBudget = null,
        string partition = "partition",
        FailureKind failureKind = FailureKind.Handling,
        string message = "It failed") =>
        new(id, partition, _now - failingFor, _now - TimeSpan.FromSeconds(10), attemptCount, attemptsInCurrentBudget ?? attemptCount, isQuarantined, failureKind, message);

    protected ObserverAlertSnapshot SnapshotOf(params FailedPartitionSnapshot[] failedPartitions) =>
        new(_observer, failedPartitions, false, AlertDisposition.Active, 10) { QuarantineEpisodeId = _defaultQuarantineEpisodeId };

    protected ObserverAlertEvaluation Evaluate(ObserverAlertSnapshot snapshot, params OpenIncident[] openIncidents) =>
        _evaluator.Evaluate(snapshot, openIncidents, _now);

    protected static OpenIncident OpenPartitionIncident(FailedPartitionId id, AlertConditionKind? condition = null, AlertSeverity severity = AlertSeverity.Warning, string partition = "partition") =>
        new(id, condition ?? AlertConditionKind.PartitionFailing, severity, partition);

    protected static OpenIncident OpenQuarantineIncident(IncidentId? id = null) =>
        new(id ?? new IncidentId(_defaultQuarantineEpisodeId), AlertConditionKind.ObserverQuarantined, AlertSeverity.Critical, AlertPartition.None);
}
