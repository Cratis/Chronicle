// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Services.Alerts;

/// <summary>
/// Converts incident query values to generated nested transport messages.
/// </summary>
internal static class AlertIncidentConverters
{
    /// <summary>
    /// Converts complete incident details.
    /// </summary>
    /// <param name="source">The domain details.</param>
    /// <returns>The transport details.</returns>
    public static Contracts.Alerts.AlertIncidentDetails ToContract(this AlertIncidentDetails source) => new()
    {
        Id = source.Id,
        Target = new()
        {
            EventStore = source.Target.EventStore,
            Namespace = source.Target.Namespace,
            ObserverId = source.Target.ObserverId,
            EventSequenceId = source.Target.EventSequenceId,
            Partition = source.Target.Partition
        },
        Condition = source.Condition,
        Severity = (Contracts.Alerts.AlertSeverity)source.Severity,
        Evidence = new()
        {
            AttemptCount = source.Evidence.AttemptCount,
            FirstFailure = source.Evidence.FirstFailure,
            LastFailure = source.Evidence.LastFailure,
            FailureKind = (Contracts.Observation.FailureKind)source.Evidence.FailureKind,
            Message = source.Evidence.Message
        },
        RaisedAt = source.RaisedAt,
        LastChangedAt = source.LastChangedAt,
        RaisedSequenceNumber = source.RaisedSequenceNumber,
        LastTransitionSequenceNumber = source.LastTransitionSequenceNumber
    };

    /// <summary>
    /// Converts a continuation.
    /// </summary>
    /// <param name="source">The domain continuation.</param>
    /// <returns>The transport continuation.</returns>
    public static Contracts.Alerts.AlertIncidentContinuation ToContract(this AlertIncidentContinuation source) => new()
    {
        RaisedSequenceNumber = source.RaisedSequenceNumber,
        IncidentId = source.IncidentId
    };

    /// <summary>
    /// Converts a count bucket.
    /// </summary>
    /// <param name="source">The domain count.</param>
    /// <returns>The transport count.</returns>
    public static Contracts.Alerts.AlertIncidentCountDetails ToContract(this AlertIncidentCountDetails source) => new()
    {
        Namespace = source.Namespace,
        Condition = source.Condition,
        Severity = (Contracts.Alerts.AlertSeverity)source.Severity,
        Count = source.Count
    };
}
