// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Converts recorded alert events and their persisted context to incident transitions.
/// </summary>
public static class AlertIncidentTransitionConverters
{
    /// <summary>
    /// Converts a recorded transition.
    /// </summary>
    /// <param name="source">Recorded event.</param>
    /// <param name="context">Persisted context.</param>
    /// <returns>The incident transition.</returns>
    public static AlertIncidentTransition ToTransition(this AlertRaised source, EventContext context) => new(
        AlertIncidentTransitionKind.Raised,
        source.IncidentId,
        new(source.Target.EventStore, source.Target.Namespace, source.Target.ObserverId, source.Target.EventSequenceId, source.Target.Partition),
        source.Condition,
        source.Severity,
        new(source.Evidence.AttemptCount, source.Evidence.FirstFailure, source.Evidence.LastFailure, source.Evidence.FailureKind, source.Evidence.Message),
        null,
        context.Occurred,
        context.SequenceNumber);

    /// <summary>
    /// Converts a recorded transition.
    /// </summary>
    /// <param name="source">Recorded event.</param>
    /// <param name="context">Persisted context.</param>
    /// <returns>The incident transition.</returns>
    public static AlertIncidentTransition ToTransition(this AlertEscalated source, EventContext context) => new(
        AlertIncidentTransitionKind.Escalated,
        source.IncidentId,
        new(source.Target.EventStore, source.Target.Namespace, source.Target.ObserverId, source.Target.EventSequenceId, source.Target.Partition),
        source.Condition,
        source.Severity,
        new(source.Evidence.AttemptCount, source.Evidence.FirstFailure, source.Evidence.LastFailure, source.Evidence.FailureKind, source.Evidence.Message),
        null,
        context.Occurred,
        context.SequenceNumber);

    /// <summary>
    /// Converts a recorded transition.
    /// </summary>
    /// <param name="source">Recorded event.</param>
    /// <param name="context">Persisted context.</param>
    /// <returns>The incident transition.</returns>
    public static AlertIncidentTransition ToTransition(this AlertCleared source, EventContext context) => new(
        AlertIncidentTransitionKind.Cleared,
        source.IncidentId,
        new(source.Target.EventStore, source.Target.Namespace, source.Target.ObserverId, source.Target.EventSequenceId, source.Target.Partition),
        source.Condition,
        null,
        null,
        source.Reason,
        context.Occurred,
        context.SequenceNumber);
}
