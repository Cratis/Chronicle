// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Converts retained open rows to public query values outside query-bearing read models.
/// </summary>
public static class AlertIncidentConverters
{
    /// <summary>
    /// Converts a complete open row to query details.
    /// </summary>
    /// <param name="source">Complete open row.</param>
    /// <returns>Query details.</returns>
    public static AlertIncidentDetails ToDetails(this AlertIncident source) => new(
        source.Id,
        new(source.Target.EventStore, source.Target.Namespace, source.Target.ObserverId, source.Target.EventSequenceId, source.Target.Partition),
        source.Condition,
        source.Severity!.Value,
        new(source.Evidence!.AttemptCount, source.Evidence.FirstFailure, source.Evidence.LastFailure, source.Evidence.FailureKind, source.Evidence.Message),
        source.RaisedAt!.Value,
        source.LastChangedAt,
        source.RaisedSequenceNumber!,
        source.LastTransitionSequenceNumber);

    /// <summary>
    /// Converts a complete count bucket.
    /// </summary>
    /// <param name="source">Count bucket.</param>
    /// <returns>Query count.</returns>
    public static AlertIncidentCountDetails ToDetails(this AlertIncidentCount source) => new(source.Namespace, source.Condition, source.Severity, source.Count);

    /// <summary>
    /// Converts a storage continuation.
    /// </summary>
    /// <param name="source">Storage continuation.</param>
    /// <returns>Query continuation.</returns>
    public static AlertIncidentContinuation ToContinuation(this AlertIncidentCursor source) => new(source.RaisedSequenceNumber, source.IncidentId);
}
