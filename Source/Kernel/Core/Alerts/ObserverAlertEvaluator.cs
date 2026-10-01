// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Works out which alert transitions an observer needs, by comparing what is true of it with the incidents that are
/// open for it.
/// </summary>
/// <remarks>
/// <para>
/// The evaluator is pure: it reads no state and appends nothing. It returns the events to append and when the next
/// raise falls due, and whoever calls it decides how to deliver them. Because it compares a snapshot with the open
/// incidents rather than reacting to a notification, evaluating twice - or after a restart, or after a missed
/// notification - never produces a duplicate raise or loses a clear.
/// </para>
/// <para>
/// The rules, per condition:
/// </para>
/// <list type="bullet">
/// <item>A partition incident has the identifier of the failed partition. <c language="csharp">partition-failing</c> raises once the
/// partition has been failing for <see cref="AlertCondition.RaiseAfter"/> while still being retried; a partition that
/// recovers earlier raises nothing.</item>
/// <item>A quarantined partition is never retried automatically, so it always needs a person. When the partition is
/// quarantined the same incident is escalated to <c language="csharp">partition-retries-exhausted</c>. A quarantined partition with
/// no open incident raises <c language="csharp">partition-retries-exhausted</c> straight away, without waiting for the grace period.
/// A partition whose attempts exceed a positive maximum is also exhausted, even if observer-wide quarantine prevented
/// the partition flag from being set. At exactly the maximum, one more retry is still allowed. With a maximum of 0
/// (retry forever), only the partition quarantine flag makes it exhausted.</item>
/// <item>An incident clears with the episode's ending hint (or Recovered after a crash) as soon as its partition is no
/// longer failing.</item>
/// <item><c language="csharp">observer-quarantined</c> raises when the observer is quarantined, with a new incident identifier, and
/// clears with the episode's ending hint (or Cleared after a crash) when it no longer is.</item>
/// <item>A removed observer raises nothing, and every incident it has open clears with
/// <see cref="AlertClearedReason.Removed"/>.</item>
/// </list>
/// <para>
/// Disabling a condition, excluding an observer and disabling alerts as a whole all stop new raises and escalations.
/// They never clear an incident that is already open: it stays open until its state ends, so turning an alert off
/// does not announce that the problem is gone.
/// </para>
/// </remarks>
/// <param name="conditions">The <see cref="IAlertConditions"/> in effect.</param>
[Singleton]
public class ObserverAlertEvaluator(IAlertConditions conditions)
{
    /// <summary>
    /// Evaluates an observer.
    /// </summary>
    /// <param name="snapshot">The <see cref="ObserverAlertSnapshot"/> saying what is true of the observer now.</param>
    /// <param name="openIncidents">The <see cref="OpenIncident"/> items already open for the observer.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The <see cref="ObserverAlertEvaluation"/> with the transitions to append.</returns>
    public ObserverAlertEvaluation Evaluate(
        ObserverAlertSnapshot snapshot,
        IReadOnlyCollection<OpenIncident> openIncidents,
        DateTimeOffset now)
    {
        if (snapshot.Disposition != AlertDisposition.Active)
        {
            return new([.. openIncidents.Select(incident => Cleared(snapshot, incident, AlertClearedReason.Removed))], null);
        }

        var transitions = new List<object>();
        transitions.AddRange(ClearEndedIncidents(snapshot, openIncidents));

        var incidentsById = new Dictionary<IncidentId, OpenIncident>();
        foreach (var incident in openIncidents)
        {
            incidentsById.TryAdd(incident.Id, incident);
        }

        DateTimeOffset? nextRaiseDue = null;
        if (!IsLeftOut(snapshot.Observer.ObserverId))
        {
            foreach (var partition in snapshot.FailedPartitions)
            {
                incidentsById.TryGetValue(partition.Id, out var incident);
                var (transition, due) = EvaluatePartition(snapshot, partition, incident, now);
                if (transition is not null)
                {
                    transitions.Add(transition);
                }

                nextRaiseDue = Earliest(nextRaiseDue, due);
            }

            if (EvaluateQuarantine(snapshot, openIncidents, now) is { } quarantine)
            {
                transitions.Add(quarantine);
            }
        }

        return new(transitions, nextRaiseDue);
    }

    static DateTimeOffset? Earliest(DateTimeOffset? current, DateTimeOffset? candidate) =>
        (current, candidate) switch
        {
            (null, _) => candidate,
            (_, null) => current,
            _ => current < candidate ? current : candidate
        };

    static bool IsExhausted(ObserverAlertSnapshot snapshot, FailedPartitionSnapshot partition) =>
        partition.IsQuarantined || (snapshot.MaxRetryAttempts > 0 && partition.AttemptCount > snapshot.MaxRetryAttempts);

    static AlertCleared Cleared(ObserverAlertSnapshot snapshot, OpenIncident incident, AlertClearedReason reason) =>
        new(incident.Id, incident.Condition, reason, AlertTarget.For(snapshot.Observer, incident.Partition));

    static AlertEvidence EvidenceFor(FailedPartitionSnapshot partition) =>
        AlertEvidence.Create(partition.AttemptCount, partition.FirstAttempt, partition.LastAttempt, partition.FailureKind, partition.Message);

    static AlertEvidence QuarantineEvidence(ObserverAlertSnapshot snapshot, DateTimeOffset now)
    {
        // The observer can be quarantined without any failed partition, for example when a catch-up job could not be
        // started. Then there is nothing to say about attempts, and the moment of raising stands in for the failure times.
        var latest = snapshot.FailedPartitions.OrderByDescending(_ => _.LastAttempt).FirstOrDefault();
        return latest is null
            ? AlertEvidence.Create(0, now, now, FailureKind.Unknown, string.Empty)
            : EvidenceFor(latest);
    }

    static IEnumerable<AlertCleared> ClearEndedIncidents(ObserverAlertSnapshot snapshot, IReadOnlyCollection<OpenIncident> openIncidents)
    {
        var failedPartitionIds = snapshot.FailedPartitions.Select(partition => (IncidentId)partition.Id).ToHashSet();
        foreach (var incident in openIncidents)
        {
            if (incident.Condition == AlertConditionKind.ObserverQuarantined)
            {
                if (!snapshot.IsQuarantined || snapshot.QuarantineEpisodeId != incident.Id.Value)
                {
                    yield return Cleared(snapshot, incident, snapshot.Endings.GetValueOrDefault(incident.Id.Value, AlertClearedReason.Cleared));
                }
            }
            else if (IsPartitionCondition(incident.Condition) && !failedPartitionIds.Contains(incident.Id))
            {
                yield return Cleared(snapshot, incident, snapshot.Endings.GetValueOrDefault(incident.Id.Value, AlertClearedReason.Recovered));
            }
        }
    }

    static bool IsPartitionCondition(AlertConditionKind kind) =>
        kind == AlertConditionKind.PartitionFailing || kind == AlertConditionKind.PartitionRetriesExhausted;

    (object? Transition, DateTimeOffset? RaiseDue) EvaluatePartition(
        ObserverAlertSnapshot snapshot,
        FailedPartitionSnapshot partition,
        OpenIncident? incident,
        DateTimeOffset now)
    {
        var failing = conditions.For(AlertConditionKind.PartitionFailing);
        var exhausted = conditions.For(AlertConditionKind.PartitionRetriesExhausted);
        var target = AlertTarget.For(snapshot.Observer, partition.Partition);

        if (incident is null)
        {
            if (IsExhausted(snapshot, partition) && exhausted.Enabled)
            {
                return (new AlertRaised(partition.Id, exhausted.Kind, exhausted.Severity, target, EvidenceFor(partition)), null);
            }

            if (!failing.Enabled)
            {
                return (null, null);
            }

            var due = partition.FirstAttempt + failing.RaiseAfter;
            return due <= now
                ? (new AlertRaised(partition.Id, failing.Kind, failing.Severity, target, EvidenceFor(partition)), null)
                : (null, due);
        }

        if (!IsPartitionCondition(incident.Condition))
        {
            return (null, null);
        }

        if (IsExhausted(snapshot, partition) && exhausted.Enabled && incident.Condition != exhausted.Kind)
        {
            return (new AlertEscalated(incident.Id, exhausted.Kind, exhausted.Severity, target, EvidenceFor(partition)), null);
        }

        return (null, null);
    }

    AlertRaised? EvaluateQuarantine(ObserverAlertSnapshot snapshot, IReadOnlyCollection<OpenIncident> openIncidents, DateTimeOffset now)
    {
        var condition = conditions.For(AlertConditionKind.ObserverQuarantined);
        if (!snapshot.IsQuarantined || snapshot.QuarantineEpisodeId is null || !condition.Enabled ||
            openIncidents.Any(_ => _.Condition == condition.Kind && _.Id.Value == snapshot.QuarantineEpisodeId))
        {
            return null;
        }

        return new AlertRaised(
            new IncidentId(snapshot.QuarantineEpisodeId.Value),
            condition.Kind,
            condition.Severity,
            AlertTarget.For(snapshot.Observer, AlertPartition.None),
            QuarantineEvidence(snapshot, now));
    }

    bool IsLeftOut(ObserverId observerId) =>
        observerId.Value.StartsWith(AlertObservers.Prefix, StringComparison.Ordinal) || conditions.IsExcluded(observerId);
}
