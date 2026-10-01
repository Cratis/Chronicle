// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents the outcome of evaluating an observer for alerts.
/// </summary>
/// <param name="Transitions">The <see cref="AlertRaised"/>, <see cref="AlertEscalated"/> and <see cref="AlertCleared"/> events to append, in order.</param>
/// <param name="NextRaiseDue">When a raise that is still waiting for its grace period falls due, or null when nothing is waiting.</param>
public record ObserverAlertEvaluation(IReadOnlyList<object> Transitions, DateTimeOffset? NextRaiseDue)
{
    /// <summary>
    /// Works out which incidents are open once <see cref="Transitions"/> have been appended.
    /// </summary>
    /// <param name="openIncidents">The incidents that were open before the transitions.</param>
    /// <returns>The incidents that are open afterwards.</returns>
    public IReadOnlyCollection<OpenIncident> ApplyTo(IReadOnlyCollection<OpenIncident> openIncidents)
    {
        var result = openIncidents.ToList();
        foreach (var transition in Transitions)
        {
            switch (transition)
            {
                case AlertRaised raised:
                    result.Add(new(raised.IncidentId, raised.Condition, raised.Severity, raised.Target.Partition));
                    break;

                case AlertEscalated escalated:
                    result = [.. result.Select(_ => _.Id == escalated.IncidentId ? _ with { Condition = escalated.Condition, Severity = escalated.Severity } : _)];
                    break;

                case AlertCleared cleared:
                    result.RemoveAll(_ => _.Id == cleared.IncidentId);
                    break;
            }
        }

        return result;
    }
}
