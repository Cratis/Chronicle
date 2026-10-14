// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// A delivery row with queryable fencing facts and immutable ingress JSON.
/// </summary>
public class OccurrenceEntry
{
    /// <summary>
    /// Gets or sets the identity hash.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trigger hash.
    /// </summary>
    public string Trigger { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the materializing schedule hash, independently of the trigger identity.
    /// </summary>
    public string? Schedule { get; set; }

    /// <summary>
    /// Gets or sets the ingress JSON.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state.
    /// </summary>
    public OccurrenceState State { get; set; }

    /// <summary>
    /// Gets or sets the fencing epoch.
    /// </summary>
    public long Epoch { get; set; }

    /// <summary>
    /// Gets or sets the attempt count.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the worker.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets the UTC expiry ticks.
    /// </summary>
    public long? Expiry { get; set; }

    /// <summary>
    /// Gets or sets the UTC retry ticks.
    /// </summary>
    public long? NextAttempt { get; set; }

    /// <summary>
    /// Gets or sets the last error.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Builds a row from durable ingress facts.
    /// </summary>
    /// <param name="occurrence">The occurrence.</param>
    /// <returns>The database row.</returns>
    public static OccurrenceEntry From(Occurrence occurrence) => new()
    {
        Id = StorageIdentity.Hash(occurrence.Id.Value),
        Trigger = StorageIdentity.Hash(occurrence.Trigger.Value),
        Data = JsonSerializer.Serialize(occurrence),
        State = occurrence.State,
        Epoch = occurrence.Epoch,
        Attempts = occurrence.Attempts,
        Owner = occurrence.Owner,
        Expiry = occurrence.LeaseExpiry?.UtcTicks,
        NextAttempt = occurrence.NextAttempt?.UtcTicks,
        LastError = occurrence.LastError
    };

    /// <summary>
    /// Reconstructs delivery facts independently of the original ingress JSON.
    /// </summary>
    /// <returns>The occurrence.</returns>
    public Occurrence ToOccurrence() => JsonSerializer.Deserialize<Occurrence>(Data)! with
    {
        State = State,
        Epoch = Epoch,
        Attempts = Attempts,
        Owner = Owner,
        LeaseExpiry = Expiry.HasValue ? new DateTimeOffset(Expiry.Value, TimeSpan.Zero) : null,
        NextAttempt = NextAttempt.HasValue ? new DateTimeOffset(NextAttempt.Value, TimeSpan.Zero) : null,
        LastError = LastError
    };
}
