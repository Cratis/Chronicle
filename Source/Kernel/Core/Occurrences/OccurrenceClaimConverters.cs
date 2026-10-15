// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Converts durable leases to worker delivery contracts.
/// </summary>
internal static class OccurrenceClaimConverters
{
    /// <summary>
    /// Converts a worker claim to its generated wire message.
    /// </summary>
    /// <param name="claim">The durable claim.</param>
    /// <returns>The wire claim.</returns>
    public static Contracts.Occurrences.OccurrenceClaimDetails ToContract(this OccurrenceClaimDetails claim) => new()
    {
        Id = claim.Id,
        Trigger = claim.Trigger,
        Kind = (Contracts.Storage.Occurrences.OccurrenceKind)claim.Kind,
        Payload = claim.Payload,
        SchemaRevision = claim.SchemaRevision,
        RecordedAt = claim.RecordedAt,
        Due = claim.Due.HasValue ? (Contracts.Primitives.SerializableDateTimeOffset)claim.Due.Value : null!,
        Attempts = claim.Attempts,
        Epoch = claim.Epoch,
        LeaseExpiry = claim.LeaseExpiry
    };

    /// <summary>
    /// Converts serialized grain snapshots to claim details.
    /// </summary>
    /// <param name="snapshots">The claimed facts.</param>
    /// <returns>The worker claims.</returns>
    internal static IEnumerable<OccurrenceClaimDetails> FromJson(string snapshots) => JsonSerializer.Deserialize<Occurrence[]>(snapshots)!.Select(occurrence => new OccurrenceClaimDetails(
        occurrence.Id.Value, occurrence.Trigger.Value, occurrence.Kind, occurrence.Payload, occurrence.SchemaRevision, occurrence.RecordedAt, occurrence.Due, occurrence.Attempts, occurrence.Epoch, occurrence.LeaseExpiry!.Value)).ToArray();
}
