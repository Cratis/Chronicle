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
/// This holds whatever the maximum number of retries is. When the maximum number of retries is 0 (retry forever) a
/// partition that is not quarantined keeps being retried and is never treated as exhausted by itself.</item>
/// <item>An incident clears with <see cref="AlertClearedReason.Recovered"/> as soon as its partition is no longer
/// failing.</item>
/// <item><c language="csharp">observer-quarantined</c> raises when the observer is quarantined, with a new incident identifier, and
/// clears with <see cref="ObserverAlertSnapshot.QuarantineEndedAs"/> when it no longer is.</item>
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
    /// The prefix of the identifiers of the kernel's own alert observers.
    /// </summary>
    /// <remarks>
    /// A kernel reactor is given the identifier <c language="csharp">$system.</c> followed by its reactor identifier, which defaults to the
    /// full name of its type. The alert observers live in the <c language="csharp">Cratis.Chronicle.Alerts</c> namespace, so that is the
    /// prefix that is always left out of alerting: an alert observer that fails must not raise an alert about itself.
    /// Other kernel observers, such as the event store subscriptions, are alerted on like any other.
    /// </remarks>
    public const string AlertObserverPrefix = "$system.Cratis.Chronicle.Alerts.";

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
        if (snapshot.IsRemoved)
        {
            return new([.. openIncidents.Select(incident => Cleared(snapshot, incident, AlertClearedReason.Removed))], null);
        }

        var transitions = new List<object>();
        transitions.AddRange(ClearEndedIncidents(snapshot, openIncidents));

        DateTimeOffset? nextRaiseDue = null;
        if (!IsLeftOut(snapshot.Observer.ObserverId))
        {
            foreach (var partition in snapshot.FailedPartitions)
            {
                var (transition, due) = EvaluatePartition(snapshot, partition, openIncidents.FirstOrDefault(_ => _.Id == partition.Id), now);
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

    static bool IsExhausted(FailedPartitionSnapshot partition) => partition.IsQuarantined;

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

    IEnumerable<AlertCleared> ClearEndedIncidents(ObserverAlertSnapshot snapshot, IReadOnlyCollection<OpenIncident> openIncidents)
    {
        foreach (var incident in openIncidents)
        {
            if (incident.Condition == AlertConditionKind.ObserverQuarantined)
            {
                if (!snapshot.IsQuarantined)
                {
                    yield return Cleared(snapshot, incident, snapshot.QuarantineEndedAs);
                }
            }
            else if (snapshot.FailedPartitions.All(_ => _.Id != incident.Id))
            {
                yield return Cleared(snapshot, incident, AlertClearedReason.Recovered);
            }
        }
    }

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
            if (IsExhausted(partition) && exhausted.Enabled)
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

        if (IsExhausted(partition) && exhausted.Enabled && incident.Condition != exhausted.Kind)
        {
            return (new AlertEscalated(incident.Id, exhausted.Kind, exhausted.Severity, target, EvidenceFor(partition)), null);
        }

        return (null, null);
    }

    AlertRaised? EvaluateQuarantine(ObserverAlertSnapshot snapshot, IReadOnlyCollection<OpenIncident> openIncidents, DateTimeOffset now)
    {
        var condition = conditions.For(AlertConditionKind.ObserverQuarantined);
        if (!snapshot.IsQuarantined || !condition.Enabled || openIncidents.Any(_ => _.Condition == condition.Kind))
        {
            return null;
        }

        return new AlertRaised(
            IncidentId.New(),
            condition.Kind,
            condition.Severity,
            AlertTarget.For(snapshot.Observer, AlertPartition.None),
            QuarantineEvidence(snapshot, now));
    }

    bool IsLeftOut(ObserverId observerId) =>
        observerId.Value.StartsWith(AlertObserverPrefix, StringComparison.Ordinal) || conditions.IsExcluded(observerId);
}
