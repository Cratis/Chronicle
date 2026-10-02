// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Folds recorded transitions into retained rows without consulting operational state.
/// </summary>
public static class AlertIncidentFold
{
    /// <summary>
    /// Applies a transition monotonically per incident.
    /// </summary>
    /// <param name="current">Existing row, if any.</param>
    /// <param name="transition">Recorded transition.</param>
    /// <returns>The retained row and application outcome.</returns>
    public static AlertIncidentFoldResult Apply(AlertIncident? current, AlertIncidentTransition transition)
    {
        if (current is not null && current.LastTransitionSequenceNumber >= transition.SequenceNumber)
        {
            return new(current, AlertIncidentWriteOutcome.AlreadyApplied);
        }
        if (transition.Kind == AlertIncidentTransitionKind.Escalated && (current?.IsOpen != true))
        {
            return new(current, AlertIncidentWriteOutcome.OrphanEscalation);
        }

        var incident = transition.Kind switch
        {
            AlertIncidentTransitionKind.Raised => new AlertIncident(transition.Id, transition.Target, transition.Condition, transition.Severity, transition.Evidence, transition.Occurred, transition.SequenceNumber, transition.Occurred, transition.SequenceNumber, true, null),
            AlertIncidentTransitionKind.Escalated => current! with
            {
                Condition = transition.Condition,
                Severity = transition.Severity,
                Evidence = transition.Evidence,
                LastChangedAt = transition.Occurred,
                LastTransitionSequenceNumber = transition.SequenceNumber
            },
            _ => new AlertIncident(transition.Id, transition.Target, transition.Condition, current?.Severity, current?.Evidence, current?.RaisedAt, current?.RaisedSequenceNumber, transition.Occurred, transition.SequenceNumber, false, transition.ClearedReason)
        };

        return new(incident, AlertIncidentWriteOutcome.Applied);
    }
}
