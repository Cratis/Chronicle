// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents the outcome of evaluating an observer for alerts.
/// </summary>
/// <param name="Transitions">The transitions to append, with clears first.</param>
/// <param name="NextRaiseDue">The next grace deadline, if any.</param>
public record ObserverAlertEvaluation(IReadOnlyList<object> Transitions, DateTimeOffset? NextRaiseDue)
{
    /// <summary>
    /// Folds the transitions into the open incidents.
    /// </summary>
    /// <param name="openIncidents">The previously open incidents.</param>
    /// <returns>The resulting open incidents.</returns>
    public IReadOnlyCollection<OpenIncident> ApplyTo(IReadOnlyCollection<OpenIncident> openIncidents)
    {
        var result = openIncidents.ToDictionary(_ => _.Id);
        foreach (var transition in Transitions)
        {
            Apply(transition, result);
        }

        return result.Values.ToArray();
    }

    internal static void Apply(object transition, IDictionary<IncidentId, OpenIncident> incidents)
    {
        switch (transition)
        {
            case AlertRaised raised:
                incidents[raised.IncidentId] = new(raised.IncidentId, raised.Condition, raised.Severity, raised.Target.Partition);
                break;
            case AlertEscalated escalated when incidents.TryGetValue(escalated.IncidentId, out var incident):
                incidents[escalated.IncidentId] = incident with { Condition = escalated.Condition, Severity = escalated.Severity };
                break;
            case AlertCleared cleared:
                incidents.Remove(cleared.IncidentId);
                break;
        }
    }
}
