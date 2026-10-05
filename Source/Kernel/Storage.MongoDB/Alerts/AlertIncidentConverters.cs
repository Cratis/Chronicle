// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Storage.MongoDB.Alerts;

/// <summary>
/// Converts retained rows to and from flattened provider values.
/// </summary>
public static class AlertIncidentConverters
{
    /// <summary>
    /// Converts a row to a provider value.
    /// </summary>
    /// <param name="source">The row.</param>
    /// <returns>The flattened value.</returns>
    public static AlertIncidentDocument ToMongoDB(this AlertIncident source) => new()
    {
        Id = AlertIncidentStorageRules.Key(source.Id),
        EventStore = source.Target.EventStore.Value,
        Namespace = source.Target.Namespace.Value,
        ObserverId = source.Target.ObserverId.Value,
        EventSequenceId = source.Target.EventSequenceId.Value,
        Partition = source.Target.Partition.Value,
        Condition = source.Condition.Value,
        Severity = (int?)source.Severity,
        AttemptCount = source.Evidence?.AttemptCount,
        FirstFailure = source.Evidence?.FirstFailure.ToString("O", CultureInfo.InvariantCulture),
        LastFailure = source.Evidence?.LastFailure.ToString("O", CultureInfo.InvariantCulture),
        FailureKind = (int?)source.Evidence?.FailureKind,
        Message = source.Evidence?.Message,
        RaisedAt = source.RaisedAt?.ToString("O", CultureInfo.InvariantCulture),
        RaisedSequenceNumber = source.RaisedSequenceNumber?.Value,
        LastChangedAt = source.LastChangedAt.ToString("O", CultureInfo.InvariantCulture),
        LastTransitionSequenceNumber = source.LastTransitionSequenceNumber.Value,
        IsOpen = source.IsOpen,
        ClearedReason = (int?)source.ClearedReason
    };

    /// <summary>
    /// Converts a provider value to a retained row.
    /// </summary>
    /// <param name="source">The flattened value.</param>
    /// <returns>The retained row.</returns>
    public static AlertIncident ToKernel(this AlertIncidentDocument source) => new(
        new IncidentId(Guid.ParseExact(source.Id, "N")),
        new(source.EventStore, source.Namespace, source.ObserverId, source.EventSequenceId, source.Partition),
        source.Condition,
        (AlertSeverity?)source.Severity,
        source.AttemptCount is null ? null : new(source.AttemptCount.Value, DateTimeOffset.Parse(source.FirstFailure!, CultureInfo.InvariantCulture), DateTimeOffset.Parse(source.LastFailure!, CultureInfo.InvariantCulture), (FailureKind)source.FailureKind!.Value, source.Message!),
        source.RaisedAt is null ? null : DateTimeOffset.Parse(source.RaisedAt, CultureInfo.InvariantCulture),
        source.RaisedSequenceNumber is null ? null : new EventSequenceNumber((ulong)source.RaisedSequenceNumber.Value),
        DateTimeOffset.Parse(source.LastChangedAt, CultureInfo.InvariantCulture),
        new EventSequenceNumber((ulong)source.LastTransitionSequenceNumber),
        source.IsOpen,
        (AlertClearedReason?)source.ClearedReason);
}
